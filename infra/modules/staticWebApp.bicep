// Static Web App hosting the Angular SPA. Content is uploaded by deploy-frontend.yml with the
// deployment token, so the site is not linked to the repository here.
param location string
param tags object
param staticWebAppName string

resource staticWebApp 'Microsoft.Web/staticSites@2024-04-01' = {
  name: staticWebAppName
  location: location
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
  }
}

output name string = staticWebApp.name
output defaultHostname string = staticWebApp.properties.defaultHostname
