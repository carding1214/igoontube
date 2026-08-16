param(
    [Parameter(Mandatory)][string]$ProjectRoot,
    [Parameter(Mandatory)][string]$Staging,
    [switch]$NoAI
)
$ErrorActionPreference = 'Stop'

function Copy-Tree([string]$source, [string]$destination) {
    New-Item -ItemType Directory -Force $destination | Out-Null
    Copy-Item -Path (Join-Path $source '*') -Destination $destination -Recurse -Force
}

New-Item -ItemType Directory -Force $Staging | Out-Null
$readme = if ($NoAI) { 'IgoonTube-NoAI-README.txt' } else { 'IgoonTube-LEEME.txt' }
Copy-Item -LiteralPath (Join-Path $ProjectRoot "docs\$readme") -Destination (Join-Path $Staging 'LEEME.txt') -Force
if ($NoAI) { exit 0 }

$basePython = Get-ChildItem (Join-Path $ProjectRoot '.tools\uv-python') -Directory -Filter 'cpython-3.11*windows-*' -ErrorAction SilentlyContinue |
    Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'python.exe') } | Select-Object -First 1
if ($basePython) {
    Copy-Tree $basePython.FullName (Join-Path $Staging '.tools\ai\python')
    Copy-Tree (Join-Path $ProjectRoot '.tools\ai\.venv\Lib\site-packages') (Join-Path $Staging '.tools\ai\python\Lib\site-packages')
} else {
    Copy-Tree (Join-Path $ProjectRoot '.tools\ai') (Join-Path $Staging '.tools\ai')
}
Copy-Tree (Join-Path $ProjectRoot 'data\models') (Join-Path $Staging 'data\models')
New-Item -ItemType Directory -Force (Join-Path $Staging 'scripts') | Out-Null
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'scripts\vision_host.py') -Destination (Join-Path $Staging 'scripts\vision_host.py') -Force
