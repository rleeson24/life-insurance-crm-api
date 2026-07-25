# Deploy dev infrastructure. Prompts for SQL password and writes a temporary .bicepparam
# file so special characters are not mangled by PowerShell and az accepts the override.
param(
    [string]$ResourceGroup = 'rg-licrm-dev',
    [string]$Location = 'centralus'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$exists = az group exists --name $ResourceGroup
if ($exists -eq 'false') {
    Write-Host "Creating resource group $ResourceGroup in $Location..."
    az group create --name $ResourceGroup --location $Location | Out-Null
}

$securePassword = Read-Host 'SQL admin password' -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
try {
    $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
}

# az does not allow a JSON @parameters file alongside a .bicepparam file — merge password into a temp .bicepparam.
$baseParamsPath = Join-Path $repoRoot 'infra/parameters/dev.bicepparam'
$localParamsPath = Join-Path $repoRoot 'infra/parameters/dev.local.bicepparam'
$escapedPassword = $plainPassword -replace "'", "''"
$paramContent = Get-Content -Path $baseParamsPath -Raw
$paramContent = $paramContent -replace "param sqlAdministratorLoginPassword = ''", "param sqlAdministratorLoginPassword = '$escapedPassword'"
Set-Content -Path $localParamsPath -Value $paramContent -Encoding utf8 -NoNewline

try {
    az deployment group create `
        --resource-group $ResourceGroup `
        --template-file infra/main.bicep `
        --parameters $localParamsPath

    if ($LASTEXITCODE -ne 0) {
        throw "Deployment failed (exit code $LASTEXITCODE)."
    }

    Write-Host ""
    Write-Host "Deployment outputs:"
    az deployment group show `
        --resource-group $ResourceGroup `
        --name main `
        --query properties.outputs `
        -o json
} finally {
    Remove-Item $localParamsPath -ErrorAction SilentlyContinue
    $plainPassword = $null
}
