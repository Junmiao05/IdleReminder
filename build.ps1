$ErrorActionPreference='Stop'
Set-Location $PSScriptRoot
$env:DOTNET_CLI_HOME=Join-Path $PSScriptRoot '.tools'
$env:NUGET_PACKAGES=Join-Path $PSScriptRoot '.tools\packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
dotnet build src/IdleReminder.csproj -c Release
if($LASTEXITCODE){throw '编译失败'}
dotnet run --project tests/EngineTests.csproj -c Release
if($LASTEXITCODE){throw '测试失败'}
dotnet publish src/IdleReminder.csproj -c Release -r win-x64 --self-contained true -o publish/current
if($LASTEXITCODE){throw '发布失败'}
