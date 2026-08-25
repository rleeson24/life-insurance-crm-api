# Deploy dev infrastructure. Prompts for SQL password and writes a temporary .bicepparam
# file so special characters are not mangled by PowerShell and az accepts the override.
param(
    [string]$ResourceGroup = 'rg-licrm-dev',
    [string]$Location = 'centralus',
    [string]$SubscriptionId = '605a6796-5cf0-4a61-80f0-ff2d484360ee'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$currentAccount = az account show --query "{name:name, id:id}" -o json | ConvertFrom-Json
if ($currentAccount.id -ne $SubscriptionId) {
    Write-Host "Switching subscription from $($currentAccount.name) ($($currentAccount.id))"
    Write-Host "                  to target $SubscriptionId"
    az account set --subscription $SubscriptionId | Out-Null
    $currentAccount = az account show --query "{name:name, id:id}" -o json | ConvertFrom-Json
}

Write-Host "Subscription: $($currentAccount.name) ($($currentAccount.id))"

$exists = az group exists --name $ResourceGroup
if ($exists -eq 'false') {
    Write-Host "Creating resource group $ResourceGroup in $Location..."
    Write-Host "Note: SQL server and ACR names are globally unique; a new RG gets auto-generated names."
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

if ($paramContent -match "param keyVaultSecretsOfficerPrincipalId = ''") {
    $officerId = az ad signed-in-user show --query id -o tsv
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($officerId)) {
        throw "Could not resolve the signed-in user's object ID. Set keyVaultSecretsOfficerPrincipalId in infra/parameters/dev.bicepparam."
    }
    Write-Host "Granting Key Vault Secrets Officer to signed-in user $officerId"
    $paramContent = $paramContent -replace "param keyVaultSecretsOfficerPrincipalId = ''", "param keyVaultSecretsOfficerPrincipalId = '$officerId'"
}

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
