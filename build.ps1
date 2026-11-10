$ErrorActionPreference='Stop'
Set-Location $PSScriptRoot
$env:DOTNET_CLI_HOME=Join-Path $PSScriptRoot '.tools'
$env:NUGET_PACKAGES=Join-Path $PSScriptRoot '.tools\packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
dotnet publish src/IdleReminder.csproj -c Release -r win-x64 --self-contained true -o publish/current
if($LASTEXITCODE){throw '编译发布失败'}
foreach($name in @('LICENSE','LICENSE_SCOPE.md','ICON-LICENSE.md','测试与兼容性说明.md')){
 Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination 'publish/current' -Force
}
Copy-Item -LiteralPath 'licenses','themes' -Destination 'publish/current' -Recurse -Force
Write-Output '完成：publish/current/IdleReminder.exe。请保留整个目录。'