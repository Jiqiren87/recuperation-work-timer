$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$installDir = Join-Path $root '.dotnet'
$script = Join-Path $env:TEMP 'dotnet-install.ps1'

if (-not (Test-Path $script)) {
    Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $script -UseBasicParsing
}

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
& powershell -NoProfile -ExecutionPolicy Bypass -File $script -Channel 8.0 -InstallDir $installDir -NoPath

$dotnet = Join-Path $installDir 'dotnet.exe'
if (-not (Test-Path $dotnet)) {
    throw 'dotnet-install.ps1 did not produce .dotnet\dotnet.exe'
}

& $dotnet --version
Write-Host "Installed local .NET SDK at $installDir"
