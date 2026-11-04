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

// ---- Entra ID (API app registration validated by the JWT middleware) ----
@description('Entra ID tenant ID used to validate access tokens.')
param azureAdTenantId string = tenant().tenantId

@description('Client ID of the API app registration.')
param azureAdClientId string

@description('Optional token audience (e.g. api://<client-id>). Empty = default validation.')
param azureAdAudience string = ''

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
}

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
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    keyVaultName: keyVault.outputs.name
    demoAuthSecretUri: keyVault.outputs.demoAuthSecretUri
    demoAuthEnabled: demoAuthEnabled
    sqlConnectionString: sql.outputs.connectionString
    azureAdTenantId: azureAdTenantId
    azureAdClientId: azureAdClientId
    azureAdAudience: azureAdAudience
    frontendUrl: 'https://${staticWebApp.outputs.defaultHostname}'
    corsAllowedOrigins: union([
      'https://${staticWebApp.outputs.defaultHostname}'
    ], extraCorsOrigins)
    enableScmBasicAuth: enableScmBasicAuth
    maximumInstanceCount: functionMaxInstances
  }
}

output functionAppName string = functionApp.outputs.name
output functionAppUrl string = 'https://${functionApp.outputs.defaultHostname}/api'
output functionAppPrincipalId string = functionApp.outputs.principalId
output staticWebAppName string = staticWebApp.outputs.name
output staticWebAppUrl string = 'https://${staticWebApp.outputs.defaultHostname}'
output sqlServerFqdn string = sql.outputs.fullyQualifiedDomainName
output sqlDatabaseName string = sql.outputs.databaseName
output keyVaultName string = keyVault.outputs.name
