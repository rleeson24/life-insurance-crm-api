targetScope = 'subscription'

// Stable id. Keep in sync with githubDeployerRoleDefinitionId in infra/main.bicep.
// Assignable at subscription scope so each environment can assign it on its own resource group.
// Assignment scope is the resource group; this role does not grant subscription-wide access.
var roleDefinitionId = 'c4e8a1d6-7b32-4f90-9e15-6a0d3c8b2f47'

resource githubDeployerRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' = {
  name: roleDefinitionId
  properties: {
    roleName: 'BrokerBook GitHub Deployer'
    description: 'Manage BrokerBook resources. Cannot delete SQL, long-term backups, Log Analytics, diagnostic settings, or resource locks; cannot purge Key Vault or export the database.'
    type: 'CustomRole'
    permissions: [
      {
        actions: [
          '*'
        ]
        notActions: [
          // Same authorization limits as Contributor.
          'Microsoft.Authorization/*/Delete'
          'Microsoft.Authorization/*/Write'
          'Microsoft.Authorization/elevateAccess/Action'
          'Microsoft.Blueprint/blueprintAssignments/write'
          'Microsoft.Blueprint/blueprintAssignments/delete'
          'Microsoft.Compute/galleries/share/action'
          // Deploy identity must not destroy data, backups, or the audit trail.
          'Microsoft.Sql/servers/delete'
          'Microsoft.Sql/servers/databases/delete'
          'Microsoft.Sql/locations/longTermRetentionServers/longTermRetentionDatabases/longTermRetentionBackups/delete'
          'Microsoft.Sql/servers/databases/export/action'
          'Microsoft.KeyVault/locations/deletedVaults/purge/action'
          'Microsoft.OperationalInsights/workspaces/delete'
          'Microsoft.Insights/diagnosticSettings/delete'
          'Microsoft.Authorization/locks/delete'
        ]
      }
    ]
    assignableScopes: [
      subscription().id
    ]
  }
}

output roleDefinitionId string = roleDefinitionId
