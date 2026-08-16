$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$uvDir = Join-Path $projectRoot '.tools\uv'
$uv = Join-Path $uvDir 'uv.exe'
$venv = Join-Path $projectRoot '.tools\ai\.venv'
$python = Join-Path $venv 'Scripts\python.exe'
$download = Join-Path $projectRoot 'work\downloads\uv.zip'
$extract = Join-Path $projectRoot 'work\downloads\uv'

$env:UV_CACHE_DIR = Join-Path $projectRoot 'data\uv-cache'
$env:UV_PYTHON_INSTALL_DIR = Join-Path $projectRoot '.tools\uv-python'
$env:UV_PYTHON_BIN_DIR = Join-Path $projectRoot '.tools\uv-python-bin'
$env:UV_PYTHON_NO_REGISTRY = '1'
$env:UV_PYTHON_PREFERENCE = 'only-managed'
$env:UV_NO_MODIFY_PATH = '1'
$env:UV_SYSTEM_CERTS = '1'
$env:TEMP = Join-Path $projectRoot 'work\temp'
$env:TMP = $env:TEMP
$env:TORCH_HOME = Join-Path $projectRoot 'data\models\torch'
$env:XDG_CACHE_HOME = Join-Path $projectRoot 'data\cache'
$env:Path = "$projectRoot;$env:Path"

New-Item -ItemType Directory -Force $uvDir,$env:UV_CACHE_DIR,$env:UV_PYTHON_INSTALL_DIR,$env:UV_PYTHON_BIN_DIR,$env:TEMP,$env:TORCH_HOME,$env:XDG_CACHE_HOME,(Split-Path $download) | Out-Null

if (-not (Test-Path -LiteralPath $uv)) {
    Invoke-WebRequest 'https://github.com/astral-sh/uv/releases/latest/download/uv-x86_64-pc-windows-msvc.zip' -OutFile $download
    if (Test-Path -LiteralPath $extract) { Remove-Item -LiteralPath $extract -Recurse -Force }
    Expand-Archive -LiteralPath $download -DestinationPath $extract
    Copy-Item -LiteralPath (Get-ChildItem -LiteralPath $extract -Filter uv.exe -Recurse | Select-Object -First 1).FullName -Destination $uv
}

& $uv python install 3.11
if ($LASTEXITCODE -ne 0) { throw 'No se pudo instalar Python 3.11.' }
if (-not (Test-Path -LiteralPath $python)) {
    & $uv venv --python 3.11 $venv
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear el entorno IA.' }
}

& $uv pip install --python $python 'torch==2.1.2' 'torchaudio==2.1.2' --index-url 'https://download.pytorch.org/whl/cu121'
if ($LASTEXITCODE -ne 0) { throw 'No se pudo instalar PyTorch CUDA.' }
& $uv pip install --python $python 'demucs==4.0.1' 'soundfile' 'numpy<2' 'packaging'
if ($LASTEXITCODE -ne 0) { throw 'No se pudo instalar Demucs.' }

& $uv pip install --python $python 'mediapipe==0.10.35' 'opencv-contrib-python<4.12' 'numpy<2'
if ($LASTEXITCODE -ne 0) { throw 'No se pudo instalar MediaPipe.' }
$poseModel = Join-Path $projectRoot 'data\models\mediapipe\pose_landmarker_lite.task'
New-Item -ItemType Directory -Force (Split-Path $poseModel) | Out-Null
if (-not (Test-Path -LiteralPath $poseModel)) {
    Invoke-WebRequest 'https://storage.googleapis.com/mediapipe-models/pose_landmarker/pose_landmarker_lite/float16/latest/pose_landmarker_lite.task' -OutFile $poseModel
}

& (Join-Path $PSScriptRoot 'verify-ai.ps1')
