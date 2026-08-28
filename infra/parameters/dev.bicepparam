using '../main.bicep'

// Canonical subscription: 605a6796-5cf0-4a61-80f0-ff2d484360ee ("Primary")
// SQL server, ACR, and Key Vault names are auto-generated per resource group (globally unique).
// Do not set sqlServerNameOverride / acrNameOverride unless importing an existing server/registry.

param environment = 'dev'
param location = 'centralus'
param githubOwner = 'rleeson24'
// GitHub repo names for OIDC subjects — must match GitHub. Azure resources use bbcrm-*.
param githubRepository = 'life-insurance-crm-api'
param githubClientRepository = 'life-insurance-crm-client'
param sqlAdministratorLogin = 'sqladmin'
// Set at deploy time via deploy-infra-dev.ps1 (password embedded in temp .bicepparam)
param sqlAdministratorLoginPassword = ''
param sqlAzureAdAdministratorObjectId = 'e1da25de-af92-4e5c-a9ac-1bc186bb9a4f'
// Entra object ID of the operator who sets vault secrets (az ad signed-in-user show --query id -o tsv).
param keyVaultSecretsOfficerPrincipalId = 'e1da25de-af92-4e5c-a9ac-1bc186bb9a4f'

// Minimal dev sizing — scale API to zero when idle; serverless SQL auto-pauses after 60 min idle
param containerAppCpu = '0.25'
param containerAppMemory = '0.5Gi'
param containerAppMinReplicas = 0
param containerAppMaxReplicas = 1
param sqlSkuName = 'GP_S_Gen5'
param sqlSkuTier = 'GeneralPurpose'
param sqlSkuCapacity = 1
param sqlAutoPauseDelay = 60
param sqlMinCapacity = '0.5'
param logAnalyticsRetentionInDays = 30
param enableSqlAuditing = false
param enableSqlDiagnostics = false
