$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$env:DOTNET_ROOT = "$root\.tools\dotnet"
$env:DOTNET_CLI_HOME = "$root\.dotnet-home"
$env:NUGET_PACKAGES = "$root\.packages"
$env:PUPLAYER_TEST_ROOT = "$root\work\tests"
$env:TEMP = $env:TMP = "$root\work\temp"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
@($env:DOTNET_CLI_HOME, $env:NUGET_PACKAGES, $env:PUPLAYER_TEST_ROOT, $env:TEMP) |
    ForEach-Object { New-Item -ItemType Directory -Force -Path $_ | Out-Null }
& "$env:DOTNET_ROOT\dotnet.exe" @args
exit $LASTEXITCODE
