param location string
param baseName string
param tags object
param administratorLogin string
@secure()
param administratorLoginPassword string
param azureAdAdministratorObjectId string
param logAnalyticsWorkspaceId string
param privateEndpointSubnetId string
param skuName string
param skuTier string
param skuCapacity int
param autoPauseDelay int
param minCapacity string
param enableAuditing bool
param enableDiagnostics bool

var databaseName = 'LifeInsuranceCRM'
var isServerless = skuName == 'GP_S_Gen5'

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${baseName}-sql'
  location: location
  tags: tags
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Disabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  tags: tags
  sku: isServerless ? {
    name: skuName
    tier: skuTier
    family: 'Gen5'
    capacity: skuCapacity
  } : {
    name: skuName
    tier: skuTier
  }
  properties: union(
    {
      zoneRedundant: false
      readScale: 'Disabled'
    },
    isServerless ? {
      autoPauseDelay: autoPauseDelay
      minCapacity: json(minCapacity)
    } : {}
  )
}

resource transparentDataEncryption 'Microsoft.Sql/servers/databases/transparentDataEncryption@2023-08-01-preview' = {
  parent: sqlDatabase
  name: 'current'
  properties: {
    state: 'Enabled'
  }
}

resource auditingSettings 'Microsoft.Sql/servers/auditingSettings@2023-08-01-preview' = if (enableAuditing) {
  parent: sqlServer
  name: 'default'
  properties: {
    state: 'Enabled'
    isAzureMonitorTargetEnabled: true
  }
}

resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (enableDiagnostics) {
  name: '${baseName}-sql-diagnostics'
  scope: sqlDatabase
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2024-01-01' = {
  name: '${baseName}-sql-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${baseName}-sql-pls'
        properties: {
          privateLinkServiceId: sqlServer.id
          groupIds: [
            'sqlServer'
          ]
        }
      }
    ]
  }
}

resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' existing = {
  name: 'privatelink.database.windows.net'
}

resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-01-01' = {
  parent: privateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'sql'
        properties: {
          privateDnsZoneId: privateDnsZone.id
        }
      }
    ]
  }
}

resource azureAdAdministrator 'Microsoft.Sql/servers/administrators@2023-08-01-preview' = if (!empty(azureAdAdministratorObjectId)) {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: 'CRM SQL Admins'
    sid: azureAdAdministratorObjectId
    tenantId: subscription().tenantId
  }
}

output sqlServerId string = sqlServer.id
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = databaseName
