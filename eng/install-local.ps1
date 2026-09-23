param(
    [Parameter(Mandatory = $true)]
    [string] $PublishDirectory,

    [switch] $NoDesktopShortcut
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$publishRoot = [IO.Path]::GetFullPath($PublishDirectory)
$executableName = 'ScreenshotAssistant.App.exe'
$sourceExecutable = Join-Path $publishRoot $executableName
if (-not (Test-Path -LiteralPath $sourceExecutable -PathType Leaf)) {
    throw "发布目录无效，未找到 $sourceExecutable"
}

$programsRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs'))
$installRoot = [IO.Path]::GetFullPath((Join-Path $programsRoot 'ScreenshotAssistant'))
if (-not $installRoot.StartsWith($programsRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "安装目录超出允许范围：$installRoot"
}

Get-Process -Name 'ScreenshotAssistant.App' -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue

if (Test-Path -LiteralPath $installRoot) {
    Remove-Item -LiteralPath $installRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $installRoot -Force | Out-Null
Copy-Item -Path (Join-Path $publishRoot '*') -Destination $installRoot -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'uninstall-local.ps1') -Destination (Join-Path $installRoot 'Uninstall.ps1') -Force

$installedExecutable = Join-Path $installRoot $executableName
$shell = New-Object -ComObject WScript.Shell
$startMenuDirectory = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$startMenuShortcut = Join-Path $startMenuDirectory '截图助手.lnk'
$shortcut = $shell.CreateShortcut($startMenuShortcut)
$shortcut.TargetPath = $installedExecutable
$shortcut.WorkingDirectory = $installRoot
$shortcut.IconLocation = "$installedExecutable,0"
$shortcut.Description = '截图助手'
$shortcut.Save()

$desktopShortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) '截图助手.lnk'
if ($NoDesktopShortcut) {
    Remove-Item -LiteralPath $desktopShortcut -Force -ErrorAction SilentlyContinue
}
else {
    $shortcut = $shell.CreateShortcut($desktopShortcut)
    $shortcut.TargetPath = $installedExecutable
    $shortcut.WorkingDirectory = $installRoot
    $shortcut.IconLocation = "$installedExecutable,0"
    $shortcut.Description = '截图助手'
    $shortcut.Save()
}

$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ScreenshotAssistant'
New-Item -Path $uninstallKey -Force | Out-Null
$uninstallScript = Join-Path $installRoot 'Uninstall.ps1'
$uninstallCommand = 'powershell.exe -NoProfile -ExecutionPolicy Bypass -File "{0}"' -f $uninstallScript
$estimatedSize = [int]((Get-ChildItem -LiteralPath $installRoot -Recurse -File | Measure-Object Length -Sum).Sum / 1KB)
New-ItemProperty -Path $uninstallKey -Name DisplayName -Value '截图助手' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name DisplayVersion -Value '0.1.0' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name Publisher -Value 'Screenshot Assistant' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name InstallLocation -Value $installRoot -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name DisplayIcon -Value $installedExecutable -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name UninstallString -Value $uninstallCommand -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name EstimatedSize -Value $estimatedSize -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name NoModify -Value 1 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name NoRepair -Value 1 -PropertyType DWord -Force | Out-Null

$iconRefreshTool = Join-Path $env:SystemRoot 'System32\ie4uinit.exe'
if (Test-Path -LiteralPath $iconRefreshTool -PathType Leaf) {
    Start-Process -FilePath $iconRefreshTool -ArgumentList '-show' -WindowStyle Hidden -Wait
}

Start-Process -FilePath $installedExecutable
Write-Host "截图助手已安装到：$installRoot"
