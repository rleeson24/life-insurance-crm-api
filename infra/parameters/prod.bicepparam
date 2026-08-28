using '../main.bicep'

param environment = 'prod'
param location = 'centralus'
param githubOwner = 'rleeson24'
// GitHub repo names for OIDC subjects — must match GitHub. Azure resources use bbcrm-*.
param githubRepository = 'life-insurance-crm-api'
param githubClientRepository = 'life-insurance-crm-client'
param sqlAdministratorLogin = 'sqladmin'
param sqlAdministratorLoginPassword = ''
param sqlAzureAdAdministratorObjectId = ''
param keyVaultSecretsOfficerPrincipalId = ''
param containerImage = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

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
