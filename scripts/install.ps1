param(
    [switch]$NoStart
)

$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$source = Join-Path $root 'artifacts\publish\RestWorkTimer.exe'
if (-not (Test-Path $source)) {
    & (Join-Path $PSScriptRoot 'publish.ps1')
}

$destDir = Join-Path $env:LOCALAPPDATA 'Programs\RestWorkTimer'
$dest = Join-Path $destDir 'RestWorkTimer.exe'
New-Item -ItemType Directory -Force -Path $destDir | Out-Null

Get-Process RestWorkTimer -ErrorAction SilentlyContinue | Stop-Process -Force
Copy-Item $source $dest -Force

$shortcutPath = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\休养期间工作计时提醒期.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $dest
$shortcut.WorkingDirectory = $destDir
$shortcut.Description = '休养期间工作计时提醒期'
$shortcut.Save()

Write-Host "已安装到 $dest"
Write-Host "启动菜单快捷方式：$shortcutPath"
Write-Host '首次运行时，程序会按设置写入 HKCU 开机自启项（默认开启）。'

if (-not $NoStart) {
    Start-Process $dest
}
