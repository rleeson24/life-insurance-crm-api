param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Push-Location $root

try {
    dotnet restore LifeInsuranceCRM.slnx
    dotnet build LifeInsuranceCRM.slnx --no-restore --configuration $Configuration

    if (Test-Path TestResults) {
        Remove-Item -Recurse -Force TestResults
    }

    if (Test-Path CodeCoverage) {
        Remove-Item -Recurse -Force CodeCoverage
    }

    dotnet test LifeInsuranceCRM.slnx `
        --no-build `
        --configuration $Configuration `
        --settings coverlet.runsettings `
        --collect:"XPlat Code Coverage" `
        --results-directory TestResults

    dotnet tool restore

    dotnet reportgenerator `
        "-reports:TestResults/**/coverage.cobertura.xml" `
        "-targetdir:CodeCoverage/report" `
        "-reporttypes:Html;Cobertura;TextSummary"

    Write-Host ""
    Write-Host "Coverage summary:"
    Get-Content CodeCoverage/report/Summary.txt
    Write-Host ""
    Write-Host "HTML report: $root/CodeCoverage/report/index.html"
}
finally {
    Pop-Location
}
