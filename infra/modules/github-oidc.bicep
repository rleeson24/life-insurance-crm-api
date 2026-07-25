param location string
param baseName string
param tags object
param githubOwner string
param githubRepository string
param githubEnvironment string
param acrName string

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

resource deployIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${baseName}-github-deploy'
  location: location
  tags: tags
}

resource federatedCredentialMain 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: deployIdentity
  name: 'github-main'
  properties: {
    issuer: 'https://token.actions.githubusercontent.com'
    subject: 'repo:${githubOwner}/${githubRepository}:ref:refs/heads/main'
    audiences: [
      'api://AzureADTokenExchange'
    ]
  }
}

resource federatedCredentialEnvironment 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: deployIdentity
  name: 'github-environment'
  dependsOn: [
    federatedCredentialMain
  ]
  properties: {
    issuer: 'https://token.actions.githubusercontent.com'
    subject: 'repo:${githubOwner}/${githubRepository}:environment:${githubEnvironment}'
    audiences: [
      'api://AzureADTokenExchange'
    ]
  }
}

module contributorAssignment 'role-assignment.bicep' = {
  name: 'github-contributor-${uniqueString(deployIdentity.id)}'
  params: {
    principalId: deployIdentity.properties.principalId
    roleDefinitionId: 'b24988ac-6180-42a0-ab88-20f7382dd24c'
  }
}

var acrPushRoleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8311e382-0749-4cb8-b61a-304f252e45ec')

resource acrPushAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, deployIdentity.id, acrPushRoleDefinitionId)
  scope: acr
  dependsOn: [
    federatedCredentialMain
    federatedCredentialEnvironment
  ]
  properties: {
    roleDefinitionId: acrPushRoleDefinitionId
    principalId: deployIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

output identityId string = deployIdentity.id
output clientId string = deployIdentity.properties.clientId
output principalId string = deployIdentity.properties.principalId
