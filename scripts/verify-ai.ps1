$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$python = Join-Path $projectRoot '.tools\ai\.venv\Scripts\python.exe'
$env:UV_CACHE_DIR = Join-Path $projectRoot 'data\uv-cache'
$env:UV_PYTHON_INSTALL_DIR = Join-Path $projectRoot '.tools\uv-python'
$env:UV_PYTHON_BIN_DIR = Join-Path $projectRoot '.tools\uv-python-bin'
$env:TEMP = Join-Path $projectRoot 'work\temp'
$env:TMP = $env:TEMP
$env:TORCH_HOME = Join-Path $projectRoot 'data\models\torch'
$env:XDG_CACHE_HOME = Join-Path $projectRoot 'data\cache'
$poseModel = Join-Path $projectRoot 'data\models\mediapipe\pose_landmarker_lite.task'
$env:PUPLAYER_POSE_MODEL = $poseModel

if (-not (Test-Path -LiteralPath $python)) { throw "Python IA no encontrado: $python" }
if (-not (Test-Path -LiteralPath $poseModel)) { throw "Modelo de pose no encontrado: $poseModel" }

& $python -c @'
import importlib.metadata as m
import os, sys, numpy, packaging, torch, torchaudio, mediapipe as mp
assert sys.version_info[:2] == (3, 11), sys.version
assert int(numpy.__version__.split('.')[0]) < 2, numpy.__version__
assert m.version('demucs') == '4.0.1'
assert torch.__version__.startswith('2.1.2'), torch.__version__
assert torchaudio.__version__.startswith('2.1.2'), torchaudio.__version__
assert mp.__version__ == '0.10.35', mp.__version__
options = mp.tasks.vision.PoseLandmarkerOptions(
    base_options=mp.tasks.BaseOptions(model_asset_path=os.environ['PUPLAYER_POSE_MODEL']),
    running_mode=mp.tasks.vision.RunningMode.IMAGE, num_poses=4)
with mp.tasks.vision.PoseLandmarker.create_from_options(options) as detector:
    image = mp.Image(image_format=mp.ImageFormat.SRGB, data=numpy.zeros((16, 16, 3), dtype=numpy.uint8))
    assert detector.detect(image) is not None
print('python={} demucs={} torch={} torchaudio={}'.format(sys.version.split()[0], m.version('demucs'), torch.__version__, torchaudio.__version__))
print('device=' + ('cuda' if torch.cuda.is_available() else 'cpu'))
print('mediapipe=' + mp.__version__ + ' pose-model=ok')
'@
if ($LASTEXITCODE -ne 0) { throw 'El entorno IA no superó la verificación.' }
