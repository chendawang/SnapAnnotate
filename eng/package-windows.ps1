param(
    [string] $Version = '0.1.0',
    [string] $Runtime = 'win-x64',
    [string] $InnoCompiler = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "版本号必须采用 major.minor.patch 格式：$Version"
}

if ($Runtime -ne 'win-x64') {
    throw "当前安装脚本仅支持 win-x64：$Runtime"
}

$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectFile = Join-Path $projectRoot 'src\ScreenshotAssistant.App\ScreenshotAssistant.App.csproj'
$publishDirectory = Join-Path $projectRoot ".artifacts\publish\$Runtime"
$installerScript = Join-Path $projectRoot 'installer\ScreenshotAssistant.iss'
$distDirectory = Join-Path $projectRoot 'dist'
$setupFile = Join-Path $distDirectory "ScreenshotAssistant-Setup-$Version-$Runtime.exe"

if (-not (Test-Path -LiteralPath $InnoCompiler -PathType Leaf)) {
    throw "未找到 Inno Setup 编译器：$InnoCompiler"
}

& dotnet publish $projectFile `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:Version=$Version `
    -p:FileVersion="$Version.0" `
    -p:AssemblyVersion="$Version.0" `
    -p:DebugSymbols=false `
    -p:DebugType=None `
    -o $publishDirectory
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish 失败，退出码：$LASTEXITCODE"
}

New-Item -ItemType Directory -Path $distDirectory -Force | Out-Null
& $InnoCompiler '/Qp' "/DAppVersion=$Version" $installerScript
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup 编译失败，退出码：$LASTEXITCODE"
}

if (-not (Test-Path -LiteralPath $setupFile -PathType Leaf)) {
    throw "安装包生成后未找到：$setupFile"
}

$hash = Get-FileHash -LiteralPath $setupFile -Algorithm SHA256
$manifestPath = Join-Path $distDirectory 'SHA256SUMS.txt'
Set-Content -LiteralPath $manifestPath -Value "$($hash.Hash)  $([IO.Path]::GetFileName($setupFile))" -Encoding utf8

Write-Host "安装包：$setupFile"
Write-Host "SHA-256：$($hash.Hash)"
