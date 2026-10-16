param location string
param baseName string
param tags object
param retentionInDays int

@description('When true, extend total retention on compliance evidence tables (Activity, SQL audit/diagnostics).')
param enableComplianceTableRetention bool = false

@description('Interactive KQL retention on compliance tables. Days 32+ incur extended retention; App Insights tables are unchanged.')
@minValue(4)
@maxValue(730)
param complianceTableAnalyticsRetentionInDays int = 90

@description('Total retention on compliance tables (includes long-term retention after analytics period). 365 = 12 months.')
@minValue(4)
@maxValue(4383)
param complianceTableTotalRetentionInDays int = 365

@description('Override default compliance table list when needed.')
param complianceRetentionTableNames array = [
  'AzureActivity'
  'AzureDiagnostics'
  'SQLSecurityAuditEvents'
]

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${baseName}-law'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: retentionInDays
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${baseName}-appi'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
  }
}

module complianceTableRetention 'log-analytics-table-retention.bicep' = if (enableComplianceTableRetention) {
  name: 'law-compliance-retention'
  params: {
    logAnalyticsWorkspaceName: logAnalytics.name
    analyticsRetentionInDays: complianceTableAnalyticsRetentionInDays
    totalRetentionInDays: complianceTableTotalRetentionInDays
    tableNames: complianceRetentionTableNames
  }
}

output logAnalyticsWorkspaceId string = logAnalytics.id
output logAnalyticsWorkspaceName string = logAnalytics.name
output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString
