param([Parameter(Mandatory)][string]$ProjectRoot, [switch]$NoAI)
$name = if ($NoAI) { 'IgoonTube-NoAI.iss' } else { 'IgoonTube.iss' }
Join-Path $ProjectRoot "installer\$name"
