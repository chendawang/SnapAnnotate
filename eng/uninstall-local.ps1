param(
    [switch] $RemoveUserData
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$programsRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs'))
$installRoot = [IO.Path]::GetFullPath((Join-Path $programsRoot 'ScreenshotAssistant'))
$currentScriptRoot = [IO.Path]::GetFullPath($PSScriptRoot)
if (-not $currentScriptRoot.Equals($installRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "卸载脚本不在预期安装目录中：$currentScriptRoot"
}

Get-Process -Name 'ScreenshotAssistant.App' -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue

$startMenuShortcut = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\截图助手.lnk'
$desktopShortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) '截图助手.lnk'
Remove-Item -LiteralPath $startMenuShortcut -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $desktopShortcut -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ScreenshotAssistant' -Recurse -Force -ErrorAction SilentlyContinue

if ($RemoveUserData) {
    $userDataRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'ScreenshotAssistant'))
    if ($userDataRoot.Equals((Join-Path $env:LOCALAPPDATA 'ScreenshotAssistant'), [StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $userDataRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$cleanupScript = Join-Path $env:TEMP ("ScreenshotAssistant-Uninstall-{0}.ps1" -f $PID)
$cleanup = @'
param([string] $Target, [int] $ParentProcessId, [string] $SelfPath)
Wait-Process -Id $ParentProcessId -ErrorAction SilentlyContinue
$programsRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs'))
$resolvedTarget = [IO.Path]::GetFullPath($Target)
if ($resolvedTarget.StartsWith($programsRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    Remove-Item -LiteralPath $resolvedTarget -Recurse -Force -ErrorAction SilentlyContinue
}
Remove-Item -LiteralPath $SelfPath -Force -ErrorAction SilentlyContinue
'@
Set-Content -LiteralPath $cleanupScript -Value $cleanup -Encoding UTF8
Start-Process -FilePath 'powershell.exe' -WindowStyle Hidden -ArgumentList @(
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', $cleanupScript,
    '-Target', $installRoot,
    '-ParentProcessId', $PID,
    '-SelfPath', $cleanupScript
)

Write-Host '截图助手已卸载。默认保留截图、历史数据库和设置。'
