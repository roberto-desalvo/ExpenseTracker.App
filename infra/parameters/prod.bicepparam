// Production parameters. Values that differ per tenant/subscription (or are secret) are read from
// environment variables, set by .github/workflows/deploy-infra.yml from GitHub vars/secrets
// (or exported in the shell for a local deploy, see infra/README.md).
using '../main.bicep'

param appName = 'expensetrk'
param environmentName = 'prod'
param staticWebAppLocation = 'westeurope'

// To adopt the existing (manually created) Function App instead of creating a new one, set its name here
// and check the what-if output carefully before the first deploy, e.g.:
// param functionAppName = 'ExpenseTracker-Functions'

// Existing SQL server and database (referenced only, never created or modified).
param sqlServerName = readEnvironmentVariable('SQL_SERVER_NAME')
param sqlDatabaseName = readEnvironmentVariable('SQL_DATABASE_NAME')
param sqlServerResourceGroup = readEnvironmentVariable('SQL_SERVER_RESOURCE_GROUP', '')

// Entra ID app registrations are created by the template (modules/entra.bicep).
param extraSpaRedirectUris = [
  'http://localhost:5173'
]

// Apex custom domain of the Static Web App. Set it (GitHub variable CUSTOM_DOMAIN) only after the
// domain has been removed from the old Static Web App: a domain can be bound to one site at a time.
param customDomainName = readEnvironmentVariable('CUSTOM_DOMAIN', '')

// OneDrive folder watched by the import Logic App (item ID from the designer trigger metadata).
param oneDriveFolderId = 'FDF9C7785021C0DD!s8fdd505250cf42f78945c5886597afd3'
param oneDriveFolderPath = '/Transazioni'

// portal.azure.com: lets you run the functions from the portal "Test/Run" pane.
param extraCorsOrigins = [
  'https://portal.azure.com'
]
param enableScmBasicAuth = true
param functionMaxInstances = 40

param demoAuthEnabled = bool(readEnvironmentVariable('DEMO_AUTH_ENABLED', 'false'))
param demoAuthSigningKey = readEnvironmentVariable('DEMO_AUTH_SIGNING_KEY', '')

param logAnalyticsDailyQuotaGb = 1
