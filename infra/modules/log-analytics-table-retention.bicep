@description('Log Analytics workspace that receives Azure Activity, SQL audit, and SQL diagnostics.')
param logAnalyticsWorkspaceName string

@description('Interactive (Analytics plan) retention in days. Data in this window is queryable with standard KQL.')
@minValue(4)
@maxValue(730)
param analyticsRetentionInDays int

@description('Total retention in days including long-term retention. Must be >= analyticsRetentionInDays.')
@minValue(4)
@maxValue(4383)
param totalRetentionInDays int

@description('Built-in tables used for Security Schedule evidence (admin activity, SQL platform logs).')
param tableNames array = [
  'AzureActivity'
  'AzureDiagnostics'
  'SQLSecurityAuditEvents'
]

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource tables 'Microsoft.OperationalInsights/workspaces/tables@2022-10-01' = [for tableName in tableNames: {
  parent: logAnalytics
  name: tableName
  properties: {
    retentionInDays: analyticsRetentionInDays
    totalRetentionInDays: totalRetentionInDays
  }
}]
