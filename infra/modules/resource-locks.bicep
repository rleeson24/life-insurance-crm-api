// CanNotDelete locks for production data and the audit workspace.
// The GitHub deploy role cannot write Microsoft.Authorization/locks, so this file
// is applied only by deploy-infra.ps1 (the local Owner). Do not add it to main.bicep.

param sqlServerName string
param keyVaultName string
param logAnalyticsWorkspaceName string

var lockNotes = 'Production data and backups. Only a subscription Owner can remove this lock.'

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' existing = {
  name: sqlServerName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource sqlLock 'Microsoft.Authorization/locks@2020-05-01' = {
  scope: sqlServer
  name: 'cannot-delete'
  properties: {
    level: 'CanNotDelete'
    notes: lockNotes
  }
}

resource keyVaultLock 'Microsoft.Authorization/locks@2020-05-01' = {
  scope: keyVault
  name: 'cannot-delete'
  properties: {
    level: 'CanNotDelete'
    notes: lockNotes
  }
}

resource logAnalyticsLock 'Microsoft.Authorization/locks@2020-05-01' = {
  scope: logAnalytics
  name: 'cannot-delete'
  properties: {
    level: 'CanNotDelete'
    notes: lockNotes
  }
}
