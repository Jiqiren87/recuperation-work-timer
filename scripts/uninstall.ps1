param(
    [switch]$RemoveData
)

$ErrorActionPreference = 'Stop'

Get-Process RestWorkTimer -ErrorAction SilentlyContinue | Stop-Process -Force

$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
Remove-ItemProperty $runKey -Name RestWorkTimer -Force -ErrorAction SilentlyContinue

$destDir = Join-Path $env:LOCALAPPDATA 'Programs\RestWorkTimer'
if (Test-Path $destDir) {
    Remove-Item $destDir -Recurse -Force
}

$shortcutPath = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\休养期间工作计时提醒期.lnk'
Remove-Item $shortcutPath -Force -ErrorAction SilentlyContinue

if ($RemoveData) {
    Remove-Item (Join-Path $env:APPDATA 'RestWorkTimer') -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $env:LOCALAPPDATA 'RestWorkTimer') -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host '已同时删除配置和本地记录。'
}

Write-Host '已卸载。'
