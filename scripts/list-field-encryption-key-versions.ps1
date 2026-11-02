# Lists field-encryption secret versions and the wrap-key metadata.
# Enables Key Vault public network access for this machine, then turns it off.
# Does not read or print secret values.
param(
    [string]$ResourceGroup = 'rg-bbcrm-prod',
    [string]$VaultName = '',
    [string]$SubscriptionId = '605a6796-5cf0-4a61-80f0-ff2d484360ee'
)

$ErrorActionPreference = 'Stop'

az account set --subscription $SubscriptionId | Out-Null

if ([string]::IsNullOrWhiteSpace($VaultName)) {
    $VaultName = az keyvault list --resource-group $ResourceGroup --query '[0].name' -o tsv
    if ([string]::IsNullOrWhiteSpace($VaultName)) {
        throw "No Key Vault found in $ResourceGroup."
    }
}

$publicAccess = az keyvault show --name $VaultName --resource-group $ResourceGroup --query properties.publicNetworkAccess -o tsv
if ($LASTEXITCODE -ne 0) {
    throw "Key Vault $VaultName was not found in $ResourceGroup."
}

$openedPublic = $false
$clientIp = $null
try {
    if ($publicAccess -eq 'Disabled') {
        $clientIp = (Invoke-RestMethod -Uri 'https://api.ipify.org' -TimeoutSec 15).Trim()
        Write-Host "Temporarily enabling public network access on $VaultName for $clientIp..."
        az keyvault update --name $VaultName --resource-group $ResourceGroup --public-network-access Enabled --only-show-errors | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Could not enable public network access on $VaultName."
        }

        $openedPublic = $true
        az keyvault network-rule add --name $VaultName --ip-address $clientIp --only-show-errors | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Could not allow $clientIp on $VaultName."
        }

        Start-Sleep -Seconds 15
    }

    Write-Host "FieldEncryption--Key versions:"
    az keyvault secret list-versions --vault-name $VaultName --name FieldEncryption--Key --query "[].{version:id, enabled:attributes.enabled, created:attributes.created}" -o table
    if ($LASTEXITCODE -ne 0) {
        throw "Could not list FieldEncryption--Key versions."
    }

    Write-Host "FieldEncryption--BlindIndexKey versions:"
    az keyvault secret list-versions --vault-name $VaultName --name FieldEncryption--BlindIndexKey --query "[].{version:id, enabled:attributes.enabled, created:attributes.created}" -o table
    if ($LASTEXITCODE -ne 0) {
        throw "Could not list FieldEncryption--BlindIndexKey versions."
    }

    Write-Host "field-encryption key:"
    $previousErrorAction = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    az keyvault key show --vault-name $VaultName --name field-encryption --query "{enabled:attributes.enabled, created:attributes.created, version:key.kid}" -o json
    $keyExit = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorAction
    if ($keyExit -ne 0) {
        Write-Warning "Could not read field-encryption key metadata. Key Vault Secrets Officer can list secrets and cannot read keys. The wrap key is unused while FieldEncryption--Key is enabled."
    }
}
finally {
    if ($openedPublic) {
        Write-Host "Disabling public network access on $VaultName..."
        $previousErrorAction = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        if (-not [string]::IsNullOrWhiteSpace($clientIp)) {
            az keyvault network-rule remove --name $VaultName --ip-address $clientIp --only-show-errors | Out-Null
        }

        az keyvault update --name $VaultName --resource-group $ResourceGroup --public-network-access Disabled --only-show-errors | Out-Null
        $disableExit = $LASTEXITCODE
        $ErrorActionPreference = $previousErrorAction
        if ($disableExit -ne 0) {
            throw "Public network access is still enabled on $VaultName. Turn it off before leaving this session."
        }

        Write-Host 'Public network access disabled.'
    }
}
