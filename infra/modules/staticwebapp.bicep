param location string
param staticWebAppName string
param tags object

@description('Static Web Apps SKU. Free is enough for a Vite SPA (no linked Functions).')
@allowed([
  'Free'
  'Standard'
])
param sku string = 'Free'

resource staticWebApp 'Microsoft.Web/staticSites@2024-04-01' = {
  name: staticWebAppName
  location: location
  tags: tags
  sku: {
    name: sku
    tier: sku
  }
  properties: {
    provider: 'None'
    allowConfigFileUpdates: true
    stagingEnvironmentPolicy: 'Disabled'
    publicNetworkAccess: 'Enabled'
  }
}

output name string = staticWebApp.name
output id string = staticWebApp.id
output hostname string = staticWebApp.properties.defaultHostname
output origin string = 'https://${staticWebApp.properties.defaultHostname}'
output redirectUri string = 'https://${staticWebApp.properties.defaultHostname}/'
