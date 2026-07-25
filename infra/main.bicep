targetScope = 'resourceGroup'

@description('Environment name used in resource naming and tagging.')
@allowed([
  'dev'
  'prod'
])
param environment string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Short name prefix for resources, e.g. licrm-dev.')
param baseName string = 'licrm-${environment}'

@description('GitHub organization or user that runs deploy workflows.')
param githubOwner string

@description('GitHub repository name for OIDC federation.')
param githubRepository string

@description('Initial SQL administrator login. Replace with Entra-only admin after bootstrap.')
param sqlAdministratorLogin string

@secure()
@description('Initial SQL administrator password. Store in Key Vault after first deploy; do not commit.')
param sqlAdministratorLoginPassword string

@description('Optional Entra object ID for Azure AD SQL administrator. Leave empty to configure later.')
param sqlAzureAdAdministratorObjectId string = ''

@description('Container image for the API. Use a placeholder until the first CI deploy pushes to ACR.')
param containerImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('Container App CPU cores as a decimal string (0.25 minimum on Consumption).')
param containerAppCpu string = environment == 'prod' ? '0.5' : '0.25'

@description('Container App memory (0.5Gi minimum on Consumption).')
param containerAppMemory string = environment == 'prod' ? '1Gi' : '0.5Gi'

@description('Minimum API replicas. Dev uses 0 to scale to zero when idle.')
param containerAppMinReplicas int = environment == 'prod' ? 1 : 0

@description('Maximum API replicas.')
param containerAppMaxReplicas int = environment == 'prod' ? 2 : 1

@description('Azure SQL SKU name. Use GP_S_Gen5 (serverless) for dev auto-pause; Basic for lowest fixed prod cost.')
param sqlSkuName string = environment == 'prod' ? 'Basic' : 'GP_S_Gen5'

@description('Azure SQL SKU tier.')
param sqlSkuTier string = environment == 'prod' ? 'Basic' : 'GeneralPurpose'

@description('Azure SQL SKU capacity (serverless vCore count). Ignored for Basic.')
param sqlSkuCapacity int = 1

@description('Serverless auto-pause delay in minutes (-1 = disabled). Dev uses 60 to pause when idle.')
param sqlAutoPauseDelay int = environment == 'prod' ? -1 : 60

@description('Serverless minimum vCores when not paused.')
param sqlMinCapacity string = '0.5'

@description('Log Analytics retention in days. PerGB2018 SKU minimum is 30.')
param logAnalyticsRetentionInDays int = 30

@description('Send SQL audit logs to Azure Monitor. Disabled in dev to reduce ingestion cost.')
param enableSqlAuditing bool = environment == 'prod'

@description('Send SQL diagnostics to Log Analytics. Disabled in dev to reduce ingestion cost.')
param enableSqlDiagnostics bool = environment == 'prod'

var tags = {
  application: 'life-insurance-crm'
  environment: environment
  managedBy: 'bicep'
}

var acrName = replace('${baseName}acr', '-', '')

// Key Vault names are globally unique. Use resource group ID (not name) so recreated RGs get a fresh vault name.
var keyVaultName = take('licrm-${environment}-${uniqueString(subscription().id, resourceGroup().id)}', 24)

module network 'modules/network.bicep' = {
  name: 'network-${environment}'
  params: {
    location: location
    baseName: baseName
    tags: tags
  }
}

module monitor 'modules/monitor.bicep' = {
  name: 'monitor-${environment}'
  params: {
    location: location
    baseName: baseName
    tags: tags
    retentionInDays: logAnalyticsRetentionInDays
  }
}

module acr 'modules/acr.bicep' = {
  name: 'acr-${environment}'
  params: {
    location: location
    baseName: baseName
    tags: tags
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault-${environment}'
  params: {
    location: location
    baseName: baseName
    keyVaultName: keyVaultName
    enablePurgeProtection: true
    tags: tags
    privateEndpointSubnetId: network.outputs.privateEndpointSubnetId
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sql-${environment}'
  params: {
    location: location
    baseName: baseName
    tags: tags
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorLoginPassword
    azureAdAdministratorObjectId: sqlAzureAdAdministratorObjectId
    logAnalyticsWorkspaceId: monitor.outputs.logAnalyticsWorkspaceId
    privateEndpointSubnetId: network.outputs.privateEndpointSubnetId
    skuName: sqlSkuName
    skuTier: sqlSkuTier
    skuCapacity: sqlSkuCapacity
    autoPauseDelay: sqlAutoPauseDelay
    minCapacity: sqlMinCapacity
    enableAuditing: enableSqlAuditing
    enableDiagnostics: enableSqlDiagnostics
  }
}

module githubOidc 'modules/github-oidc.bicep' = {
  name: 'github-oidc-${environment}'
  dependsOn: [
    acr
  ]
  params: {
    location: location
    baseName: baseName
    tags: tags
    githubOwner: githubOwner
    githubRepository: githubRepository
    githubEnvironment: environment
    acrName: acrName
  }
}

module containerApps 'modules/containerapps.bicep' = {
  name: 'containerapps-${environment}'
  params: {
    location: location
    baseName: baseName
    tags: tags
    appSubnetId: network.outputs.appSubnetId
    logAnalyticsWorkspaceId: monitor.outputs.logAnalyticsWorkspaceId
    applicationInsightsConnectionString: monitor.outputs.applicationInsightsConnectionString
    acrLoginServer: acr.outputs.loginServer
    acrName: acrName
    containerImage: containerImage
    keyVaultUri: keyVault.outputs.keyVaultUri
    sqlServerFqdn: sql.outputs.sqlServerFqdn
    databaseName: sql.outputs.databaseName
    cpu: containerAppCpu
    memory: containerAppMemory
    minReplicas: containerAppMinReplicas
    maxReplicas: containerAppMaxReplicas
  }
}

output containerAppFqdn string = containerApps.outputs.apiFqdn
output containerAppName string = containerApps.outputs.apiName
output acrLoginServer string = acr.outputs.loginServer
output acrName string = acrName
output keyVaultUri string = keyVault.outputs.keyVaultUri
output keyVaultName string = keyVaultName
output sqlServerFqdn string = sql.outputs.sqlServerFqdn
output databaseName string = sql.outputs.databaseName
output githubDeployClientId string = githubOidc.outputs.clientId
output logAnalyticsWorkspaceId string = monitor.outputs.logAnalyticsWorkspaceId
