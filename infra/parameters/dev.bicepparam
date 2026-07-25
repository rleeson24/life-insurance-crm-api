using '../main.bicep'

param environment = 'dev'
param location = 'centralus'
param githubOwner = 'rleeson24'
param githubRepository = 'life-insurance-crm-api'
param sqlAdministratorLogin = 'sqladmin'
// Set at deploy time via deploy-infra-dev.ps1 (password embedded in temp .bicepparam)
param sqlAdministratorLoginPassword = ''
param sqlAzureAdAdministratorObjectId = ''

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
