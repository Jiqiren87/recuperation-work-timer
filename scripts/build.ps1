$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$dotnet = Join-Path $root '.dotnet\dotnet.exe'

if (-not (Test-Path $dotnet)) {
    & (Join-Path $PSScriptRoot 'install-dotnet.ps1')
}

Set-Location $root
& $dotnet restore RestWorkTimer.sln
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $dotnet build RestWorkTimer.sln -c Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $dotnet test RestWorkTimer.sln -c Release --no-build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Build and tests succeeded.'
