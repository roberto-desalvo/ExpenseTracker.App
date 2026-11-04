// Static Web App hosting the Angular SPA. Content is uploaded by deploy-frontend.yml with the
// deployment token, so the site is not linked to the repository here.
param location string
param tags object
param staticWebAppName string

@description('Optional apex custom domain (e.g. example.com), validated with a DNS TXT record. Empty = none.')
param customDomainName string = ''

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

// Apex domain: TXT validation (token in the validationToken output) + ALIAS/A record to the default hostname.
resource customDomain 'Microsoft.Web/staticSites/customDomains@2024-04-01' = if (!empty(customDomainName)) {
  parent: staticWebApp
  name: empty(customDomainName) ? 'none' : customDomainName
  properties: {
    validationMethod: 'dns-txt-token'
  }
}

output name string = staticWebApp.name
output defaultHostname string = staticWebApp.properties.defaultHostname
output customDomainValidationToken string = empty(customDomainName) ? '' : customDomain!.properties.validationToken
