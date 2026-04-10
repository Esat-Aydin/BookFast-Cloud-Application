$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-StrictMode -Version Latest

$repoRoot = git rev-parse --show-toplevel
$hookPath = Join-Path $repoRoot '.githooks\pre-commit'

if (-not (Test-Path $hookPath)) {
    throw 'Missing .githooks\pre-commit. Cannot configure git hooks.'
}

Push-Location $repoRoot
try {
    git config --local core.hooksPath .githooks
    Write-Host 'Configured core.hooksPath to .githooks.' -ForegroundColor Green
} finally {
    Pop-Location
}
