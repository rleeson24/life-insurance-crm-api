using '../main.bicep'

// Canonical subscription: 605a6796-5cf0-4a61-80f0-ff2d484360ee ("Primary")
// SQL server, ACR, and Key Vault names are auto-generated per resource group (globally unique).
// Do not set sqlServerNameOverride / acrNameOverride unless importing an existing server/registry.

param environment = 'prod'
param location = 'centralus'
param githubOwner = 'rleeson24'
// GitHub repo names for OIDC subjects — must match GitHub. Azure resources use bbcrm-*.
param githubRepository = 'life-insurance-crm-api'
param githubClientRepository = 'life-insurance-crm-client'
param sqlAdministratorLogin = 'sqladmin'
// Set at deploy time via deploy-infra.ps1 (password embedded in temp .bicepparam)
param sqlAdministratorLoginPassword = ''
param sqlAzureAdAdministratorObjectId = 'e1da25de-af92-4e5c-a9ac-1bc186bb9a4f'
// Entra object ID of the operator who sets vault secrets (az ad signed-in-user show --query id -o tsv).
param keyVaultSecretsOfficerPrincipalId = 'e1da25de-af92-4e5c-a9ac-1bc186bb9a4f'
// Overridden at deploy time to the ACR bootstrap image (imported by deploy-infra.ps1).
param containerImage = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

// Minimal prod sizing — one small always-on replica; bump sqlSkuName/sqlSkuTier to S0/Standard when Basic is too small
param containerAppCpu = '0.5'
param containerAppMemory = '1Gi'
param containerAppMinReplicas = 1
param containerAppMaxReplicas = 2
param sqlSkuName = 'Basic'
param sqlSkuTier = 'Basic'
param logAnalyticsRetentionInDays = 30
param enableSqlAuditing = true
param enableSqlDiagnostics = true
param sqlBackupStorageRedundancy = 'Geo'
param enableSqlLongTermRetention = true
param enableSecurityGuardrails = true
// High-risk activity-log alerts. Leave empty to skip them.
param securityAlertEmail = 'robert@leesontechnologies.com'
