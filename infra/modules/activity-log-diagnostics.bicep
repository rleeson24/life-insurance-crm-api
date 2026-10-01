targetScope = 'subscription'

@description('Resource id of the production Log Analytics workspace.')
param logAnalyticsWorkspaceId string

// Subscription-scoped on purpose. deploy-infra.ps1 applies this as the local Owner.
// The GitHub deploy identity is resource-group scoped and must not create it.
resource activityAdministrative 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'bbcrm-activity-administrative'
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'Administrative'
        enabled: true
      }
    ]
  }
}
