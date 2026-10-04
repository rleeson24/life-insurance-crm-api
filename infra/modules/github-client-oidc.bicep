param location string
param baseName string
param tags object
param githubOwner string
param githubRepository string
param githubEnvironment string
param acrName string
param webContainerAppName string
param containerAppName string

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

resource webContainerApp 'Microsoft.App/containerApps@2024-03-01' existing = {
  name: webContainerAppName
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: '${baseName}-cae'
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

// AcrPush — build and push the SPA image. Does not grant pull of other registries.
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

// Container Apps Contributor on the web app (update the image) and on the
// environment (join/action, which image updates require). This role does not
// grant write on the environment or on the API app.
var containerAppsContributorRoleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '358470bc-b998-42bd-ab17-a7e34c199c0f')

resource webContainerAppContributorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(webContainerApp.id, deployIdentity.id, containerAppsContributorRoleDefinitionId)
  scope: webContainerApp
  dependsOn: [
    federatedCredentialMain
    federatedCredentialEnvironment
  ]
  properties: {
    roleDefinitionId: containerAppsContributorRoleDefinitionId
    principalId: deployIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource environmentJoinAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerAppsEnvironment.id, deployIdentity.id, containerAppsContributorRoleDefinitionId)
  scope: containerAppsEnvironment
  dependsOn: [
    federatedCredentialMain
    federatedCredentialEnvironment
  ]
  properties: {
    roleDefinitionId: containerAppsContributorRoleDefinitionId
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
