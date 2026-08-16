$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$installer = Join-Path $projectRoot 'work\downloads\innosetup-7.0.2-x64.exe'
$destination = Join-Path $projectRoot '.tools\inno'
$compiler = Join-Path $destination 'ISCC.exe'
New-Item -ItemType Directory -Force (Split-Path $installer),$destination | Out-Null
if (-not (Test-Path -LiteralPath $installer)) {
    Invoke-WebRequest 'https://github.com/jrsoftware/issrc/releases/download/is-7_0_2/innosetup-7.0.2-x64.exe' -OutFile $installer
}
$signature = Get-AuthenticodeSignature -LiteralPath $installer
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Pyrsys') { throw 'Firma de Inno Setup inválida.' }
if (-not (Test-Path -LiteralPath $compiler)) {
    $process = Start-Process $installer -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',"/DIR=$destination" -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "No se pudo instalar Inno Setup ($($process.ExitCode))." }
}
Write-Output $compiler
