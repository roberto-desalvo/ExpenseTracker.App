// ExpenseTracker - Azure infrastructure (resource group scope).
// Deployed by .github/workflows/deploy-infra.yml with parameters from infra/parameters/*.bicepparam.
targetScope = 'resourceGroup'

@description('Short application name used to build resource names.')
@minLength(2)
@maxLength(12)
param appName string = 'expensetrk'

@description('Environment name (e.g. prod, dev), used in resource names and tags.')
@allowed([
  'dev'
  'test'
  'prod'
])
param environmentName string = 'prod'

@description('Location for all resources except the Static Web App.')
param location string = resourceGroup().location

@description('Location of the Static Web App (only a subset of regions is supported).')
@allowed([
  'westeurope'
  'centralus'
  'eastus2'
  'westus2'
  'eastasia'
])
param staticWebAppLocation string = 'westeurope'

// ---- Resource name overrides (empty = naming convention). Set them to adopt existing resources. ----
param functionAppName string = ''
param storageAccountName string = ''
param keyVaultName string = ''
param staticWebAppName string = ''

// ---- Frontend ----
@description('Optional apex custom domain of the Static Web App (e.g. expensetracker.lol). Empty = none.')
param customDomainName string = ''

// ---- Logic Apps ----
@description('OneDrive item ID of the folder watched by the import Logic App.')
param oneDriveFolderId string

@description('Display path of the watched OneDrive folder (designer metadata only).')
param oneDriveFolderPath string = '/Transazioni'

// ---- Entra ID (app registrations created by modules/entra.bicep) ----
@description('Extra SPA redirect URIs besides the Static Web App origin (e.g. local dev server).')
param extraSpaRedirectUris array = [
  'http://localhost:5173'
]

// ---- SQL (existing resources, referenced only: never created or modified) ----
@description('Name of the existing Azure SQL logical server.')
param sqlServerName string

@description('Name of the existing database on the SQL server.')
param sqlDatabaseName string

@description('Resource group of the existing SQL server (empty = the deployment resource group).')
param sqlServerResourceGroup string = ''

// ---- Function App ----
@description('Extra origins allowed by the Function App CORS, besides the Static Web App hostname.')
param extraCorsOrigins array = []

@description('Keep SCM basic-auth publishing credentials enabled (required by the publish-profile deploy in deploy-backend.yml).')
param enableScmBasicAuth bool = true

@description('Maximum number of Flex Consumption instances.')
@minValue(1)
@maxValue(1000)
param functionMaxInstances int = 40

@description('Enable the demo login (DemoAuth). Requires the signing key to be stored in Key Vault at least once.')
param demoAuthEnabled bool = false

@secure()
@description('Demo token signing key (>= 32 bytes). Empty = keep the secret already in Key Vault.')
param demoAuthSigningKey string = ''

@description('Daily ingestion cap of the Log Analytics workspace in GB (-1 = no cap).')
param logAnalyticsDailyQuotaGb int = 1

var suffix = uniqueString(resourceGroup().id, appName, environmentName)
var tags = {
  application: 'ExpenseTracker'
  environment: environmentName
  managedBy: 'bicep'
}

var names = {
  logAnalytics: 'log-${appName}-${environmentName}'
  appInsights: 'appi-${appName}-${environmentName}'
  plan: 'asp-${appName}-${environmentName}'
  functionApp: empty(functionAppName) ? 'func-${appName}-${environmentName}-${take(suffix, 6)}' : functionAppName
  storage: empty(storageAccountName) ? take('st${replace(appName, '-', '')}${environmentName}${suffix}', 24) : storageAccountName
  keyVault: empty(keyVaultName) ? take('kv-${appName}-${environmentName}-${suffix}', 24) : keyVaultName
  staticWebApp: empty(staticWebAppName) ? 'swa-${appName}-${environmentName}' : staticWebAppName
  logicIdentity: 'id-${appName}-logic-${environmentName}'
  importWorkflow: 'logic-${appName}-import-${environmentName}'
  demoResetWorkflow: 'logic-${appName}-demo-reset-${environmentName}'
}

// Origins the SPA is served from: Static Web App default hostname + optional custom domain.
var frontendOrigins = concat(
  [
    'https://${staticWebApp.outputs.defaultHostname}'
  ],
  empty(customDomainName) ? [] : ['https://${customDomainName}']
)

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    tags: tags
    logAnalyticsName: names.logAnalytics
    appInsightsName: names.appInsights
    dailyQuotaGb: logAnalyticsDailyQuotaGb
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    tags: tags
    storageAccountName: names.storage
  }
}

module staticWebApp 'modules/staticWebApp.bicep' = {
  name: 'staticWebApp'
  params: {
    location: staticWebAppLocation
    tags: tags
    staticWebAppName: names.staticWebApp
    customDomainName: customDomainName
  }
}

module entra 'modules/entra.bicep' = {
  name: 'entra'
  params: {
    appName: appName
    environmentName: environmentName
    spaRedirectUris: union(frontendOrigins, extraSpaRedirectUris)
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sql'
  scope: resourceGroup(empty(sqlServerResourceGroup) ? resourceGroup().name : sqlServerResourceGroup)
  params: {
    sqlServerName: sqlServerName
    sqlDatabaseName: sqlDatabaseName
  }
}

module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVault'
  params: {
    location: location
    tags: tags
    keyVaultName: names.keyVault
    demoAuthSigningKey: demoAuthSigningKey
  }
}

module functionApp 'modules/functionApp.bicep' = {
  name: 'functionApp'
  params: {
    location: location
    tags: tags
    planName: names.plan
    functionAppName: names.functionApp
    storageAccountName: storage.outputs.name
    deploymentContainerName: storage.outputs.deploymentContainerName
    appInsightsName: monitoring.outputs.appInsightsName
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    keyVaultName: keyVault.outputs.name
    demoAuthSecretUri: keyVault.outputs.demoAuthSecretUri
    demoAuthEnabled: demoAuthEnabled
    sqlConnectionString: sql.outputs.connectionString
    azureAdTenantId: tenant().tenantId
    azureAdClientId: entra.outputs.apiClientId
    azureAdAudience: entra.outputs.apiIdentifierUri
    frontendUrl: last(frontendOrigins) // custom domain when set
    corsAllowedOrigins: union(frontendOrigins, extraCorsOrigins)
    enableScmBasicAuth: enableScmBasicAuth
    maximumInstanceCount: functionMaxInstances
  }
}

module logicApps 'modules/logicApps.bicep' = {
  name: 'logicApps'
  params: {
    location: location
    tags: tags
    identityName: names.logicIdentity
    importWorkflowName: names.importWorkflow
    demoResetWorkflowName: names.demoResetWorkflow
    backendBaseUrl: 'https://${functionApp.outputs.defaultHostname}'
    apiAudience: entra.outputs.apiIdentifierUri
    oneDriveFolderId: oneDriveFolderId
    oneDriveFolderPath: oneDriveFolderPath
    logAnalyticsId: monitoring.outputs.logAnalyticsId
    logAnalyticsName: monitoring.outputs.logAnalyticsName
  }
}

output functionAppName string = functionApp.outputs.name
output functionAppUrl string = 'https://${functionApp.outputs.defaultHostname}/api'
output functionAppPrincipalId string = functionApp.outputs.principalId
output staticWebAppName string = staticWebApp.outputs.name
output staticWebAppUrl string = 'https://${staticWebApp.outputs.defaultHostname}'
output staticWebAppDefaultHostname string = staticWebApp.outputs.defaultHostname
output customDomainValidationToken string = staticWebApp.outputs.customDomainValidationToken
output logicAppIdentityName string = logicApps.outputs.identityName
output logicAppIdentityPrincipalId string = logicApps.outputs.identityPrincipalId
output oneDriveConnectionName string = logicApps.outputs.oneDriveConnectionName
output sqlServerFqdn string = sql.outputs.fullyQualifiedDomainName
output sqlDatabaseName string = sql.outputs.databaseName
output keyVaultName string = keyVault.outputs.name

// Values for the frontend build (deploy-frontend.yml VITE_* secrets) and the import scripts.
output tenantId string = tenant().tenantId
output apiClientId string = entra.outputs.apiClientId
output apiIdentifierUri string = entra.outputs.apiIdentifierUri
output apiScope string = entra.outputs.apiScope
output apiServicePrincipalId string = entra.outputs.apiServicePrincipalId
output filesSenderRoleId string = entra.outputs.filesSenderRoleId
output spaClientId string = entra.outputs.spaClientId
