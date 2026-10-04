param location string
param baseName string
param tags object
param appSubnetId string
param logAnalyticsWorkspaceId string
param applicationInsightsConnectionString string
param acrLoginServer string
param acrName string
param containerImage string
param keyVaultUri string
param keyVaultName string
param sqlServerFqdn string
param databaseName string
param cpu string
param memory string
param minReplicas int
param maxReplicas int

@description('When false, create the environment and ACR pull identity only. Used so the deploy script can import bootstrap images before the first revisions.')
param createApiApp bool = true

@description('Container image for the advisor UI. The deploy script imports a bootstrap nginx image into ACR so the first revision does not pull from a public registry through the VNet.')
param webContainerImage string

@description('Extra browser origins allowed to call the API, in addition to the web Container App.')
param corsAllowedOrigins array = []

var extraCorsEnv = [for (origin, i) in corsAllowedOrigins: {
  name: 'Cors__AllowedOrigins__${i + 1}'
  value: origin
}]

// Placeholder images do not serve /alive or /health. Probing them on 8080
// leaves the first revision Pending until ARM returns "Operation expired".
var isPlaceholderImage = contains(containerImage, 'containerapps-helloworld') || contains(containerImage, 'dotnet/samples') || contains(containerImage, '/bootstrap/')
// Hello-world listens on 80; the .NET sample, ACR bootstrap, and the real API listen on 8080.
var ingressTargetPort = contains(containerImage, 'containerapps-helloworld') ? 80 : 8080
var effectiveMinReplicas = isPlaceholderImage ? 0 : minReplicas
var acrPullRoleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
var apiProbes = [
  {
    type: 'Liveness'
    httpGet: {
      path: '/alive'
      port: 8080
      scheme: 'HTTP'
    }
    initialDelaySeconds: 10
    periodSeconds: 30
  }
  {
    type: 'Readiness'
    httpGet: {
      path: '/health'
      port: 8080
      scheme: 'HTTP'
    }
    initialDelaySeconds: 10
    periodSeconds: 15
  }
]

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: split(logAnalyticsWorkspaceId, '/')[8]
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource apiPullIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${baseName}-api-pull'
  location: location
  tags: tags
}

// Assign AcrPull before the Container App exists so the first revision can pull from ACR.
resource acrPullForUai 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, apiPullIdentity.id, acrPullRoleDefinitionId)
  scope: acr
  properties: {
    roleDefinitionId: acrPullRoleDefinitionId
    principalId: apiPullIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${baseName}-cae'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    vnetConfiguration: {
      infrastructureSubnetId: appSubnetId
      internal: false
    }
    zoneRedundant: false
  }
}

resource apiContainerApp 'Microsoft.App/containerApps@2024-03-01' = if (createApiApp) {
  name: '${baseName}-api'
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned, UserAssigned'
    userAssignedIdentities: {
      '${apiPullIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: ingressTargetPort
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: [
        {
          server: acrLoginServer
          identity: apiPullIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: containerImage
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          probes: isPlaceholderImage ? [] : apiProbes
          env: concat(
            [
              {
                name: 'ASPNETCORE_ENVIRONMENT'
                value: 'Production'
              }
              {
                name: 'AllowedHosts'
                value: '*'
              }
              {
                name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
                value: applicationInsightsConnectionString
              }
              {
                name: 'Database__Server'
                value: sqlServerFqdn
              }
              {
                name: 'Database__Name'
                value: databaseName
              }
              {
                name: 'KeyVault__VaultUri'
                value: keyVaultUri
              }
            ],
            concat(
              [
                {
                  name: 'Cors__AllowedOrigins__0'
                  value: 'https://${webContainerApp!.properties.configuration.ingress.fqdn}'
                }
              ],
              extraCorsEnv
            )
          )
        }
      ]
      scale: {
        minReplicas: effectiveMinReplicas
        maxReplicas: maxReplicas
      }
    }
  }
  dependsOn: [
    acrPullForUai
  ]
}

var keyVaultSecretsUserRoleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
var keyVaultCryptoUserRoleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12338af0-0e69-4776-bea7-57ae8d297424')

resource keyVaultSecretsUserAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (createApiApp) {
  name: guid(keyVault.id, apiContainerApp!.id, keyVaultSecretsUserRoleDefinitionId)
  scope: keyVault
  properties: {
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId
    principalId: apiContainerApp!.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource keyVaultCryptoUserAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (createApiApp) {
  name: guid(keyVault.id, apiContainerApp!.id, keyVaultCryptoUserRoleDefinitionId)
  scope: keyVault
  properties: {
    roleDefinitionId: keyVaultCryptoUserRoleDefinitionId
    principalId: apiContainerApp!.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource webContainerApp 'Microsoft.App/containerApps@2024-03-01' = if (createApiApp) {
  name: '${baseName}-web'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${apiPullIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 80
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: [
        {
          server: acrLoginServer
          identity: apiPullIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'web'
          image: webContainerImage
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/'
                port: 80
                scheme: 'HTTP'
              }
              initialDelaySeconds: 5
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/'
                port: 80
                scheme: 'HTTP'
              }
              initialDelaySeconds: 5
              periodSeconds: 10
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
  dependsOn: [
    acrPullForUai
  ]
}

output apiName string = createApiApp ? apiContainerApp!.name : ''
output apiFqdn string = createApiApp ? apiContainerApp!.properties.configuration.ingress.fqdn : ''
output apiIdentityPrincipalId string = createApiApp ? apiContainerApp!.identity.principalId : ''
output apiPullIdentityId string = apiPullIdentity.id
output webName string = createApiApp ? webContainerApp!.name : ''
output webFqdn string = createApiApp ? webContainerApp!.properties.configuration.ingress.fqdn : ''
