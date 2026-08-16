$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$required = @(
    "$root\.tools\dotnet\dotnet.exe",
    "$root\vendor\mpv\libmpv-2.dll"
)

$missing = $required | Where-Object { -not (Test-Path -LiteralPath $_ -PathType Leaf) }
if ($missing) { throw "Dependencias faltantes:`n$($missing -join "`n")" }

Write-Host 'Bootstrap verificado.'
