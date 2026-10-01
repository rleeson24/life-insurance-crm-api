@description('Short name prefix, e.g. bbcrm-prod.')
param baseName string

@description('Operator email for high-risk activity-log alerts.')
param securityAlertEmail string

var operations = [
  {
    name: 'role-assign-write'
    operationName: 'Microsoft.Authorization/roleAssignments/write'
  }
  {
    name: 'role-assign-delete'
    operationName: 'Microsoft.Authorization/roleAssignments/delete'
  }
  {
    name: 'tde-write'
    operationName: 'Microsoft.Sql/servers/databases/transparentDataEncryption/write'
  }
  {
    name: 'sql-audit-write'
    operationName: 'Microsoft.Sql/servers/auditingSettings/write'
  }
  {
    name: 'sql-audit-delete'
    operationName: 'Microsoft.Sql/servers/auditingSettings/delete'
  }
  {
    name: 'sql-db-audit-write'
    operationName: 'Microsoft.Sql/servers/databases/auditingSettings/write'
  }
  {
    name: 'sql-db-audit-delete'
    operationName: 'Microsoft.Sql/servers/databases/auditingSettings/delete'
  }
  {
    name: 'diagnostics-write'
    operationName: 'Microsoft.Insights/diagnosticSettings/write'
  }
  {
    name: 'diagnostics-delete'
    operationName: 'Microsoft.Insights/diagnosticSettings/delete'
  }
  {
    name: 'sql-server-delete'
    operationName: 'Microsoft.Sql/servers/delete'
  }
  {
    name: 'sql-database-delete'
    operationName: 'Microsoft.Sql/servers/databases/delete'
  }
  {
    name: 'keyvault-delete'
    operationName: 'Microsoft.KeyVault/vaults/delete'
  }
  {
    name: 'log-analytics-delete'
    operationName: 'Microsoft.OperationalInsights/workspaces/delete'
  }
  {
    name: 'lock-delete'
    operationName: 'Microsoft.Authorization/locks/delete'
  }
  {
    name: 'sql-export'
    operationName: 'Microsoft.Sql/servers/databases/export/action'
  }
]

resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: '${baseName}-security'
  location: 'global'
  properties: {
    groupShortName: 'bbcrmsec'
    enabled: true
    emailReceivers: [
      {
        name: 'security-operator'
        emailAddress: securityAlertEmail
        useCommonAlertSchema: true
      }
    ]
  }
}

resource alerts 'Microsoft.Insights/activityLogAlerts@2020-10-01' = [for item in operations: {
  name: '${baseName}-${item.name}'
  location: 'global'
  properties: {
    description: 'Succeeded ${item.operationName} in this resource group.'
    enabled: true
    scopes: [
      resourceGroup().id
    ]
    condition: {
      allOf: [
        {
          field: 'category'
          equals: 'Administrative'
        }
        {
          field: 'operationName'
          equals: item.operationName
        }
        {
          field: 'status'
          equals: 'Succeeded'
        }
      ]
    }
    actions: {
      actionGroups: [
        {
          actionGroupId: actionGroup.id
        }
      ]
    }
  }
}]
