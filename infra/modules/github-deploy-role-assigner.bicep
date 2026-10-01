targetScope = 'resourceGroup'

@description('Principal ID of the GitHub deploy user-assigned identity (bbcrm-<env>-github-deploy).')
param principalId string

// Role Based Access Control Administrator, constrained to the roles this stack assigns.
// Do not put this assignment in the GitHub Actions deployment: that identity cannot grant it to itself.
// When you add a role assignment anywhere under infra/, add its role definition ID here and re-run deploy-infra.ps1.
var roleBasedAccessControlAdministratorRoleId = 'f58310d9-a9f6-439a-9e8d-f62e7b41a168'
var allowedRoleDefinitionIds = [
  'b24988ac-6180-42a0-ab88-20f7382dd24c' // Contributor — GitHub deploy (resource group) and client deploy (Static Web App)
  '8311e382-0749-4cb8-b61a-304f252e45ec' // AcrPush — GitHub deploy identity
  '7f951dda-4ed3-4680-a7ca-43fe172d538d' // AcrPull — API pull identity
  '4633458b-17de-408a-b874-0445c86b69e6' // Key Vault Secrets User — API
  '12338af0-0e69-4776-bea7-57ae8d297424' // Key Vault Crypto User — API
  'b86a8fe4-44ce-4948-aee5-eccb2c155cd7' // Key Vault Secrets Officer — operator
  'acdd72a7-3385-48ef-bd42-f606fba81ae7' // Reader — client deploy identity on the API
]
var allowedRoleGuidList = join(allowedRoleDefinitionIds, ', ')
var roleAssignmentCondition = '((!(ActionMatches{\'Microsoft.Authorization/roleAssignments/write\'})) OR (@Request[Microsoft.Authorization/roleAssignments:RoleDefinitionId] ForAnyOfAnyValues:GuidEquals {${allowedRoleGuidList}})) AND ((!(ActionMatches{\'Microsoft.Authorization/roleAssignments/delete\'})) OR (@Resource[Microsoft.Authorization/roleAssignments:RoleDefinitionId] ForAnyOfAnyValues:GuidEquals {${allowedRoleGuidList}}))'

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, principalId, roleBasedAccessControlAdministratorRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleBasedAccessControlAdministratorRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
    description: 'GitHub infra deploy can assign only the roles declared in this Bicep stack.'
    condition: roleAssignmentCondition
    conditionVersion: '2.0'
  }
}
