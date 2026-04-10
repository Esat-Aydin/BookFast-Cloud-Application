$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiDirectory = Join-Path $repoRoot 'src\api'
$projectPath = 'BookFast.API\BookFast.API.csproj'
$locationPushed = $false

try {
    if (-not (Test-Path (Join-Path $apiDirectory 'dotnet-tools.json'))) {
        throw 'Missing src\api\dotnet-tools.json. Cannot run dotnet-format.'
    }

    Push-Location $apiDirectory
    $locationPushed = $true

    Write-Host 'Running .NET format check...' -ForegroundColor Cyan
    dotnet tool restore --verbosity minimal
    dotnet tool run dotnet-format -- $projectPath --check
} catch {
    Write-Host ''
    Write-Host 'Commit blocked because the API format check failed.' -ForegroundColor Yellow
    Write-Host 'Run the formatter and stage the updated files:' -ForegroundColor Yellow
    Write-Host '  Push-Location src\api' -ForegroundColor Yellow
    Write-Host '  dotnet tool restore' -ForegroundColor Yellow
    Write-Host '  dotnet tool run dotnet-format -- BookFast.API\BookFast.API.csproj' -ForegroundColor Yellow
    Write-Host '  Pop-Location' -ForegroundColor Yellow
    throw
} finally {
    if ($locationPushed) {
        Pop-Location
    }
}
