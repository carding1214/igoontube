$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$tools = "$root\.tools"
$temp = "$root\work\temp"
$env:TEMP = $env:TMP = $temp
$env:DOTNET_CLI_HOME = "$root\.dotnet-home"
$env:NUGET_PACKAGES = "$root\.packages"
@($tools, $temp, $env:DOTNET_CLI_HOME, $env:NUGET_PACKAGES) |
    ForEach-Object { New-Item -ItemType Directory -Force -Path $_ | Out-Null }

$dotnet = "$tools\dotnet\dotnet.exe"
if (-not (Test-Path -LiteralPath $dotnet)) {
    $installer = "$temp\dotnet-install.ps1"
    Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer
    & powershell -NoProfile -ExecutionPolicy Bypass -File $installer -Channel 8.0 -InstallDir "$tools\dotnet" -NoPath
    if ($LASTEXITCODE) { throw "dotnet-install falló: $LASTEXITCODE" }
}

$mpvDir = "$root\vendor\mpv"
$libmpv = "$mpvDir\libmpv-2.dll"
$mpvUrl = $null
if (-not (Test-Path -LiteralPath $libmpv)) {
    $headers = @{ 'User-Agent' = 'IgoonTube-bootstrap' }
    $release = Invoke-RestMethod -Headers $headers 'https://api.github.com/repos/shinchiro/mpv-winbuild-cmake/releases/latest'
    $asset = $release.assets | Where-Object { $_.name -match '^mpv-dev-x86_64-[0-9]{8}-git-[^.]+\.7z$' } | Select-Object -First 1
    if (-not $asset) { throw 'No se encontró el paquete libmpv x64.' }
    $mpvUrl = $asset.browser_download_url
    $archive = "$temp\$($asset.name)"
    $sevenZip = "$tools\7zr.exe"
    Invoke-WebRequest $mpvUrl -OutFile $archive
    if (-not (Test-Path -LiteralPath $sevenZip)) {
        Invoke-WebRequest 'https://www.7-zip.org/a/7zr.exe' -OutFile $sevenZip
    }
    $extract = "$temp\mpv-dev"
    New-Item -ItemType Directory -Force -Path $extract | Out-Null
    & $sevenZip x $archive "-o$extract" -y | Out-Null
    if ($LASTEXITCODE) { throw "Extracción de libmpv falló: $LASTEXITCODE" }
    $source = Get-ChildItem $extract -Recurse -Filter libmpv-2.dll | Select-Object -First 1
    if (-not $source) { throw 'libmpv-2.dll no apareció en el paquete.' }
    New-Item -ItemType Directory -Force -Path $mpvDir | Out-Null
    Copy-Item -LiteralPath $source.FullName -Destination $libmpv
}

$lock = [ordered]@{
    dotnet = [ordered]@{ version = (& $dotnet --version); sha256 = (Get-FileHash $dotnet -Algorithm SHA256).Hash }
    libmpv = [ordered]@{ url = $mpvUrl; sha256 = (Get-FileHash $libmpv -Algorithm SHA256).Hash }
}
$lock | ConvertTo-Json -Depth 4 | Set-Content "$tools\dependencies.lock.json" -Encoding utf8
Write-Host 'Dependencias portátiles listas.'
