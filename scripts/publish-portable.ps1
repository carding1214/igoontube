param([switch]$NoAI)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$gitDirectory = (& git -C $projectRoot rev-parse --path-format=absolute --git-common-dir).Trim()
if ($LASTEXITCODE -ne 0) { throw 'No se pudo localizar el repositorio.' }
$repositoryRoot = Split-Path -Parent $gitDirectory
$distRoot = Join-Path $repositoryRoot 'dist'
$packageName = if ($NoAI) { 'IgoonTube-NoAI' } else { 'IgoonTube' }
$target = Join-Path $distRoot $packageName
$staging = Join-Path $distRoot "$packageName.staging"

if (-not $target.StartsWith($distRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Destino de publicacion invalido.' }
if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
New-Item -ItemType Directory -Force $staging | Out-Null

$arguments = @('publish', (Join-Path $projectRoot 'src\PUPlayer.App\PUPlayer.App.csproj'), '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '--no-restore', '-o', (Join-Path $staging 'app'))
if ($NoAI) { $arguments += '--property:NoAI=true' }
& (Join-Path $PSScriptRoot 'dotnet.ps1') @arguments
if ($LASTEXITCODE -ne 0) { throw 'Fallo la publicacion .NET.' }

function Copy-Tree([string]$source, [string]$destination) {
    New-Item -ItemType Directory -Force $destination | Out-Null
    & robocopy $source $destination /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -gt 7) { throw "No se pudo copiar $source." }
}

function Move-Package([string]$source, [string]$destination) {
    foreach ($attempt in 1..10) {
        try { Move-Item -LiteralPath $source -Destination $destination -ErrorAction Stop; return }
        catch { if ($attempt -eq 10) { throw }; Start-Sleep -Milliseconds 300 }
    }
}

Copy-Tree (Join-Path $staging 'app') $staging
Remove-Item -LiteralPath (Join-Path $staging 'app') -Recurse -Force
$workerOutput = Join-Path $projectRoot 'src\PUPlayer.MpvWorker\bin\Release\net8.0\win-x64'
Copy-Item -LiteralPath (Join-Path $workerOutput 'PUPlayer.MpvWorker.exe'),(Join-Path $workerOutput 'PUPlayer.MpvWorker.dll'),(Join-Path $workerOutput 'PUPlayer.MpvWorker.deps.json'),(Join-Path $workerOutput 'PUPlayer.MpvWorker.runtimeconfig.json') -Destination $staging -Force
& (Join-Path $PSScriptRoot 'stage-optional-assets.ps1') -ProjectRoot $projectRoot -Staging $staging -NoAI:$NoAI

if (-not $NoAI) {
    $pythonRoot = Join-Path $staging '.tools\ai\python'
    $removeDirectories = @(
        (Join-Path $pythonRoot 'include'), (Join-Path $pythonRoot 'libs'), (Join-Path $pythonRoot 'tcl'),
        (Join-Path $pythonRoot 'Lib\ensurepip'), (Join-Path $pythonRoot 'Lib\idlelib'),
        (Join-Path $pythonRoot 'Lib\tkinter'), (Join-Path $pythonRoot 'Lib\site-packages\torch\include'),
        (Join-Path $pythonRoot 'Lib\site-packages\pip'), (Join-Path $pythonRoot 'Lib\site-packages\setuptools')
    )
    $torchTesting = Join-Path $pythonRoot 'Lib\site-packages\torch\testing'
    $removeDirectories += Get-ChildItem -LiteralPath $pythonRoot -Recurse -Directory |
        Where-Object { $_.Name -in @('__pycache__', 'tests', 'test', 'testing') -and $_.FullName -ne $torchTesting } |
        Sort-Object { $_.FullName.Length } -Descending | Select-Object -ExpandProperty FullName
    foreach ($directory in $removeDirectories) {
        if (Test-Path -LiteralPath $directory) { Remove-Item -LiteralPath $directory -Recurse -Force }
    }
    Get-ChildItem -LiteralPath $staging -Recurse -File |
        Where-Object Extension -In @('.pyc', '.pdb', '.lib') | Remove-Item -Force
    $env:PYTHONDONTWRITEBYTECODE = '1'
    & (Join-Path $pythonRoot 'python.exe') -c "import torch, torchaudio, demucs, mediapipe; assert torch.cuda.is_available(); print('runtime portable: cuda ok')"
    if ($LASTEXITCODE -ne 0) { throw 'El runtime IA portable no funciona.' }
    & (Join-Path $pythonRoot 'python.exe') -m demucs.separate --help | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Demucs portable no funciona.' }
}
Get-ChildItem -LiteralPath $staging -Recurse -File -Filter '*.pdb' | Remove-Item -Force

& (Join-Path $staging 'PUPlayer.MpvWorker.exe') --self-test-mpv (Join-Path $staging 'libmpv-2.dll')
if ($LASTEXITCODE -ne 0) { throw 'El motor de video portable no funciona.' }
if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
Move-Package $staging $target
& (Join-Path $PSScriptRoot 'write-release-hashes.ps1') -DistRoot $distRoot
Write-Output $target
