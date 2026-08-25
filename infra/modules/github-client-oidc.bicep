param location string
param baseName string
param tags object
param githubOwner string
param githubRepository string
param githubEnvironment string
param staticWebAppName string
param containerAppName string

resource staticWebApp 'Microsoft.Web/staticSites@2024-04-01' existing = {
  name: staticWebAppName
}

resource containerApp 'Microsoft.App/containerApps@2024-03-01' existing = {
  name: containerAppName
}

resource deployIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${baseName}-github-client-deploy'
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

// Contributor on the Static Web App only — can list deploy secrets and upload content.
var contributorRoleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')

resource staticWebAppContributorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(staticWebApp.id, deployIdentity.id, contributorRoleDefinitionId)
  scope: staticWebApp
  dependsOn: [
    federatedCredentialMain
    federatedCredentialEnvironment
  ]
  properties: {
    roleDefinitionId: contributorRoleDefinitionId
    principalId: deployIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Reader on the API Container App so the client workflow can resolve the API FQDN for VITE_API_BASE_URL.
var readerRoleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'acdd72a7-3385-48ef-bd42-f606fba81ae7')

resource containerAppReaderAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerApp.id, deployIdentity.id, readerRoleDefinitionId)
  scope: containerApp
  dependsOn: [
    federatedCredentialMain
    federatedCredentialEnvironment
  ]
  properties: {
    roleDefinitionId: readerRoleDefinitionId
    principalId: deployIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

output identityId string = deployIdentity.id
output clientId string = deployIdentity.properties.clientId
output principalId string = deployIdentity.properties.principalId
