# Deploy BrokerBook Azure infrastructure for an environment (dev or prod).
# Prompts for the SQL password and passes it as an inline az override so special
# characters are not parsed by Bicep or mangled by PowerShell.
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'prod')]
    [string]$Environment,

    [string]$ResourceGroup = '',
    [string]$Location = 'centralus',
    [string]$SubscriptionId = '605a6796-5cf0-4a61-80f0-ff2d484360ee',
    [SecureString]$SqlAdministratorLoginPassword
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

if ([string]::IsNullOrWhiteSpace($ResourceGroup)) {
    $ResourceGroup = "rg-bbcrm-$Environment"
}

$currentAccount = az account show --query "{name:name, id:id}" -o json | ConvertFrom-Json
if ($currentAccount.id -ne $SubscriptionId) {
    Write-Host "Switching subscription from $($currentAccount.name) ($($currentAccount.id))"
    Write-Host "                  to target $SubscriptionId"
    az account set --subscription $SubscriptionId | Out-Null
    $currentAccount = az account show --query "{name:name, id:id}" -o json | ConvertFrom-Json
}

Write-Host "Subscription: $($currentAccount.name) ($($currentAccount.id))"
Write-Host "Environment:  $Environment"
Write-Host "Resource group: $ResourceGroup"

$exists = az group exists --name $ResourceGroup
if ($exists -eq 'false') {
    Write-Host "Creating resource group $ResourceGroup in $Location..."
    Write-Host "Note: SQL server and ACR names are globally unique; a new RG gets auto-generated names."
    az group create --name $ResourceGroup --location $Location | Out-Null
}

$plainPassword = $null
if ($null -ne $SqlAdministratorLoginPassword) {
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdministratorLoginPassword)
    try {
        $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
} else {
    $securePassword = Read-Host 'SQL admin password' -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try {
        $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

if ([string]::IsNullOrWhiteSpace($plainPassword)) {
    throw 'SQL admin password is required.'
}

$baseParamsPath = Join-Path $repoRoot "infra/parameters/$Environment.bicepparam"
$localParamsPath = Join-Path $repoRoot "infra/parameters/$Environment.local.bicepparam"
$paramContent = Get-Content -Path $baseParamsPath -Raw
$paramsPath = $baseParamsPath
$wroteLocalParams = $false

$signedInId = $null
$resolveSignedInId = {
    if (-not $script:signedInId) {
        $script:signedInId = az ad signed-in-user show --query id -o tsv
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($script:signedInId)) {
            throw "Could not resolve the signed-in user's object ID. Set the Entra principal IDs in infra/parameters/$Environment.bicepparam."
        }
    }
    $script:signedInId
}

if ($paramContent -match "param keyVaultSecretsOfficerPrincipalId = ''") {
    $officerId = & $resolveSignedInId
    Write-Host "Granting Key Vault Secrets Officer to signed-in user $officerId"
    $paramContent = $paramContent.Replace("param keyVaultSecretsOfficerPrincipalId = ''", "param keyVaultSecretsOfficerPrincipalId = '$officerId'")
    $wroteLocalParams = $true
}

if ($paramContent -match "param sqlAzureAdAdministratorObjectId = ''") {
    $adminId = & $resolveSignedInId
    Write-Host "Setting SQL Entra administrator to signed-in user $adminId"
    $paramContent = $paramContent.Replace("param sqlAzureAdAdministratorObjectId = ''", "param sqlAzureAdAdministratorObjectId = '$adminId'")
    $wroteLocalParams = $true
}

if ($wroteLocalParams) {
    Set-Content -Path $localParamsPath -Value $paramContent -Encoding utf8 -NoNewline
    $paramsPath = $localParamsPath
}

# Inline key=value is allowed with a .bicepparam file; a JSON @file is not.
# Splatting keeps #, &, and \ inside one argv so PowerShell does not treat them as syntax.
$passwordOverride = 'sqlAdministratorLoginPassword=' + $plainPassword

function Invoke-InfraDeployment {
    param(
        [Parameter(Mandatory = $true)][string[]]$ExtraParameters
    )
    $azArgs = @(
        'deployment', 'group', 'create',
        '--resource-group', $ResourceGroup,
        '--template-file', 'infra/main.bicep',
        '--parameters', $paramsPath,
        '--parameters', $passwordOverride
    ) + $ExtraParameters
    & az @azArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Deployment failed (exit code $LASTEXITCODE)."
    }
}

try {
    $apiAppName = "bbcrm-$Environment-api"
    $apiState = az containerapp show --name $apiAppName --resource-group $ResourceGroup --query properties.provisioningState -o tsv 2>$null
    if ($LASTEXITCODE -ne 0) {
        $apiState = ''
    }
    if ($apiState -eq 'Failed') {
        Write-Host "Removing failed Container App $apiAppName so it can be recreated..."
        az containerapp delete --name $apiAppName --resource-group $ResourceGroup --yes | Out-Null
    }

    Write-Host "Creating platform resources (ACR, environment, pull identity) before the API app..."
    Invoke-InfraDeployment -ExtraParameters @('createApiContainerApp=false')

    $acrName = az acr list --resource-group $ResourceGroup --query '[0].name' -o tsv
    if ([string]::IsNullOrWhiteSpace($acrName)) {
        throw "ACR was not created in $ResourceGroup."
    }

    Write-Host "Importing bootstrap image into ACR $acrName (does not use the Container Apps VNet)..."
    az acr import `
        --name $acrName `
        --source mcr.microsoft.com/dotnet/samples:aspnetapp `
        --image bootstrap/aspnetapp:latest `
        --force
    if ($LASTEXITCODE -ne 0) {
        throw "ACR import failed (exit code $LASTEXITCODE)."
    }

    $acrLoginServer = az acr show --name $acrName --resource-group $ResourceGroup --query loginServer -o tsv
    $bootstrapImage = "$acrLoginServer/bootstrap/aspnetapp:latest"
    Write-Host "Waiting 60s for AcrPull on the pull identity to propagate..."
    Start-Sleep -Seconds 60

    Write-Host "Creating API Container App from $bootstrapImage..."
    Invoke-InfraDeployment -ExtraParameters @(
        'createApiContainerApp=true',
        ('containerImage=' + $bootstrapImage)
    )

    Write-Host ""
    Write-Host "Deployment outputs:"
    az deployment group show `
        --resource-group $ResourceGroup `
        --name main `
        --query properties.outputs `
        -o json
} finally {
    if ($wroteLocalParams) {
        Remove-Item $localParamsPath -ErrorAction SilentlyContinue
    }
    $plainPassword = $null
    $passwordOverride = $null
}
