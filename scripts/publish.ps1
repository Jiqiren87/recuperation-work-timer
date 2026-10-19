$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$dotnet = Join-Path $root '.dotnet\dotnet.exe'

if (-not (Test-Path $dotnet)) {
    & (Join-Path $PSScriptRoot 'install-dotnet.ps1')
}

$out = Join-Path $root 'artifacts\publish'
if (Test-Path $out) {
    Remove-Item $out -Recurse -Force
}

Set-Location $root
& $dotnet publish src\RestWorkTimer\RestWorkTimer.csproj `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true `
    -o $out

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Get-ChildItem $out | Select-Object Name, Length
Write-Host "Published to $out"
