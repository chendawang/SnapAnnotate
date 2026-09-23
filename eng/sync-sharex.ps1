$ErrorActionPreference = 'Stop'

$shareXCommit = 'f6d7f687673b28b1999382009ce4c0c4e9fa3258'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$shareXDirectory = Join-Path $repositoryRoot '.vendor\ShareX'
$patchFile = Join-Path $repositoryRoot 'patches\sharex\0001-custom-selection-border.patch'

if (-not (Test-Path (Join-Path $shareXDirectory '.git'))) {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $shareXDirectory) | Out-Null
    & git clone --filter=blob:none --no-checkout https://github.com/ShareX/ShareX.git $shareXDirectory
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to clone the ShareX source repository.'
    }
}

& git -C $shareXDirectory sparse-checkout init --cone
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to initialize the ShareX sparse checkout.'
}

& git -C $shareXDirectory sparse-checkout set `
    ShareX.ScreenCaptureLib `
    ShareX.Avalonia `
    ShareX.HelpersLib `
    ShareX.HistoryLib `
    ShareX.ImageEditor
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to configure the ShareX sparse checkout.'
}

& git -C $shareXDirectory cat-file -e "$shareXCommit^{commit}" 2>$null
if ($LASTEXITCODE -ne 0) {
    & git -C $shareXDirectory fetch origin $shareXCommit
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to fetch ShareX commit $shareXCommit."
    }
}

& git -C $shareXDirectory checkout --detach $shareXCommit
if ($LASTEXITCODE -ne 0) {
    throw "Unable to check out ShareX commit $shareXCommit."
}

$actualCommit = git -C $shareXDirectory rev-parse HEAD
if ($actualCommit -ne $shareXCommit) {
    throw "ShareX checkout mismatch. Expected $shareXCommit, got $actualCommit."
}

& git -C $shareXDirectory apply --check $patchFile 2>$null
if ($LASTEXITCODE -eq 0) {
    & git -C $shareXDirectory apply --whitespace=nowarn $patchFile
    if ($LASTEXITCODE -ne 0) {
        throw 'ShareX integration patch could not be applied.'
    }
}
else {
    & git -C $shareXDirectory apply --reverse --check $patchFile 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw 'ShareX sources do not match the expected patch state.'
    }
}

Write-Host "ShareX capture sources and SnapAnnotate patches are ready at $actualCommit."
