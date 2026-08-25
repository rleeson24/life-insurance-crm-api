# Grants Key Vault Secrets Officer on the vault in a resource group.
# RG Owner/Contributor cannot list or set secrets when the vault uses RBAC.
param(
    [string]$ResourceGroup = 'rg-licrm-dev',
    [string]$VaultName = '',
    [string]$PrincipalId = '',
    [string]$SubscriptionId = '605a6796-5cf0-4a61-80f0-ff2d484360ee'
)

$ErrorActionPreference = 'Stop'

az account set --subscription $SubscriptionId | Out-Null

if ([string]::IsNullOrWhiteSpace($PrincipalId)) {
    $PrincipalId = az ad signed-in-user show --query id -o tsv
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($PrincipalId)) {
        throw 'Could not resolve the signed-in user. Pass -PrincipalId <entra-object-id>.'
    }
}

if ([string]::IsNullOrWhiteSpace($VaultName)) {
    $VaultName = az keyvault list --resource-group $ResourceGroup --query '[0].name' -o tsv
    if ([string]::IsNullOrWhiteSpace($VaultName)) {
        throw "No Key Vault found in $ResourceGroup."
    }
}

$vaultId = az keyvault show --name $VaultName --resource-group $ResourceGroup --query id -o tsv
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($vaultId)) {
    throw "Key Vault $VaultName was not found in $ResourceGroup."
}

Write-Host "Assigning 'Key Vault Secrets Officer' to $PrincipalId on $VaultName..."

$createOutput = az role assignment create `
    --role 'Key Vault Secrets Officer' `
    --assignee-object-id $PrincipalId `
    --assignee-principal-type User `
    --scope $vaultId `
    --only-show-errors 2>&1

if ($LASTEXITCODE -ne 0) {
    $text = "$createOutput"
    if ($text -match 'RoleAssignmentExists|already exists') {
        Write-Host 'Role assignment already exists.'
    } else {
        throw $text
    }
} else {
    Write-Host 'Role assignment created. Wait 1-2 minutes before the portal reflects it.'
}

$publicAccess = az keyvault show --name $VaultName --resource-group $ResourceGroup --query properties.publicNetworkAccess -o tsv
if ($publicAccess -eq 'Disabled') {
    Write-Host ''
    Write-Host 'This vault has public network access disabled (private endpoint only).'
    Write-Host 'After RBAC is applied, portal/CLI from a laptop can still fail with a firewall error.'
    Write-Host 'Temporarily allow public access to set secrets, then turn it back off:'
    Write-Host "  az keyvault update --name $VaultName --resource-group $ResourceGroup --public-network-access Enabled"
    Write-Host "  az keyvault secret set --vault-name $VaultName --name AzureAd--TenantId --value <tenant-id>"
    Write-Host "  az keyvault update --name $VaultName --resource-group $ResourceGroup --public-network-access Disabled"
}
