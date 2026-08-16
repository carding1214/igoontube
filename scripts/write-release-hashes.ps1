param([string]$DistRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist'))
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $DistRoot).Path.TrimEnd('\') + '\'
$lines = Get-ChildItem -LiteralPath $DistRoot -Recurse -File |
    Where-Object Extension -In @('.exe', '.bin', '.zip') |
    Sort-Object FullName |
    ForEach-Object {
        $relative = $_.FullName.Substring($root.Length).Replace('\', '/').ToLowerInvariant()
        "$(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256 | Select-Object -ExpandProperty Hash)  $relative".ToLowerInvariant()
    }
[IO.File]::WriteAllLines((Join-Path $DistRoot 'SHA256SUMS.txt'), $lines, [Text.UTF8Encoding]::new($false))
