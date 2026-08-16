param([switch]$NoAI)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$gitDirectory = (& git -C $projectRoot rev-parse --path-format=absolute --git-common-dir).Trim()
if ($LASTEXITCODE -ne 0) { throw 'No se pudo localizar el repositorio.' }
$distRoot = Join-Path (Split-Path -Parent $gitDirectory) 'dist'
$compiler = & (Join-Path $PSScriptRoot 'bootstrap-inno.ps1')
if (-not $? -or -not (Test-Path -LiteralPath $compiler)) { throw 'No se pudo preparar Inno Setup.' }
$definition = & (Join-Path $PSScriptRoot 'select-installer.ps1') -ProjectRoot $projectRoot -NoAI:$NoAI
& $compiler $definition
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el instalador.' }
& (Join-Path $PSScriptRoot 'write-release-hashes.ps1') -DistRoot $distRoot
