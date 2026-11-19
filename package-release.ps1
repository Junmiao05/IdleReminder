param([string]$Version='0.0.1.28',[string]$DeliveryFolder='D:\IR发布包')
$ErrorActionPreference='Stop'
Set-Location $PSScriptRoot
if($Version -notmatch '^\d+\.\d+\.\d+\.\d+$'){throw '版本号格式无效'}
$source=Join-Path $PSScriptRoot "publish\IdleReminder-v$Version-win-x64.zip"
if(-not (Test-Path -LiteralPath $source)){throw '请先生成对应版本的自包含发布ZIP'}
$themeRoot=Join-Path $PSScriptRoot 'themes'
$files=@{}
Add-Type -AssemblyName System.Drawing
foreach($file in Get-ChildItem -LiteralPath $themeRoot -Filter '*.json' -File){
 if($file.Name -eq 'skin-designer-draft.json'){continue}
 if($file.Length -gt 131072){throw "皮肤文件过大：$($file.Name)"}
 $doc=Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
 if(-not $doc.Colors){throw "皮肤颜色为空：$($file.Name)"}
 foreach($color in $doc.Colors.PSObject.Properties){if($color.Value -notmatch '^#[0-9a-fA-F]{6}$'){throw "皮肤颜色无效：$($file.Name)"}}
 $files["themes/$($file.Name)"]=$file.FullName
 if($doc.CheckmarkImage){
  if($doc.CheckmarkImage -notmatch '^assets/[A-Za-z0-9_-]{1,100}\.png$'){throw "皮肤图片路径无效：$($file.Name)"}
  $asset=Join-Path $themeRoot $doc.CheckmarkImage
  $image=[Drawing.Image]::FromFile($asset)
  try{if($image.RawFormat.Guid -ne [Drawing.Imaging.ImageFormat]::Png.Guid){throw '勾号资源不是PNG'}}finally{$image.Dispose()}
  $files["themes/$($doc.CheckmarkImage)"]=$asset
 }
}
$files['themes/LICENSE.md']=Join-Path $themeRoot 'LICENSE.md'
$files['themes/GPL-3.0.txt']=Join-Path $themeRoot 'GPL-3.0.txt'
$files['LICENSE']=Join-Path $PSScriptRoot 'LICENSE'
$files['LICENSE_SCOPE.md']=Join-Path $PSScriptRoot 'LICENSE_SCOPE.md'
$files['ICON-LICENSE.md']=Join-Path $PSScriptRoot 'ICON-LICENSE.md'
foreach($license in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'licenses') -File){$files["licenses/$($license.Name)"]=$license.FullName}
# Preserve the previous ZIP inside the project before refreshing its preset resources.
$backup=Join-Path $PSScriptRoot ('backups\release-before-themes-'+(Get-Date -Format 'yyyyMMdd-HHmmssfff')+'.zip')
Copy-Item -LiteralPath $source -Destination $backup
$zip=[IO.Compression.ZipFile]::Open($source,[IO.Compression.ZipArchiveMode]::Update)
try{
 $bad=@($zip.Entries | Where-Object {$_.FullName.Replace('\','/') -match '(^|/)(data|backups|\.git|audio-source)/|(^|/)settings\.json(\.bak)?$'})
 if($bad.Count){throw '发布包含私人或开发目录，已阻止交付'}
 foreach($entry in @($zip.Entries | Where-Object {$_.FullName.Replace('\','/').StartsWith('themes/')})){$entry.Delete()}
 foreach($entry in $files.GetEnumerator()){
  $previous=$zip.GetEntry($entry.Key);if($null -ne $previous){$previous.Delete()}
  [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$entry.Value,$entry.Key,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
 }
}finally{$zip.Dispose()}
# Verify every bundled preset/resource byte-for-byte.
$zip=[IO.Compression.ZipFile]::OpenRead($source)
try{
 foreach($entry in $files.GetEnumerator()){
  $stream=$zip.GetEntry($entry.Key).Open()
  try{$actual=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream))}finally{$stream.Dispose()}
  if($actual -ne (Get-FileHash -LiteralPath $entry.Value).Hash){throw '皮肤压缩包校验失败'}
 }
}finally{$zip.Dispose()}
Copy-Item -LiteralPath $source -Destination (Join-Path $PSScriptRoot 'publish\IdleReminder-win-x64.zip') -Force
New-Item -ItemType Directory -Path $DeliveryFolder -Force | Out-Null
$target=Join-Path $DeliveryFolder "IdleReminder-v$Version-win-x64.zip"
if([IO.Path]::GetFullPath($target) -ne [IO.Path]::GetFullPath($source)){Copy-Item -LiteralPath $source -Destination $target -Force}
Write-Output "已交付：$target"
Write-Output ("皮肤JSON与所需图片：{0}个文件；草稿未收录。" -f $files.Count)