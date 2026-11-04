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

param azureAdTenantId = readEnvironmentVariable('AZURE_AD_TENANT_ID')
param azureAdClientId = readEnvironmentVariable('AZURE_AD_CLIENT_ID')
param azureAdAudience = readEnvironmentVariable('AZURE_AD_AUDIENCE', '')

param extraCorsOrigins = []
param enableScmBasicAuth = true
param functionMaxInstances = 40

param demoAuthEnabled = bool(readEnvironmentVariable('DEMO_AUTH_ENABLED', 'false'))
param demoAuthSigningKey = readEnvironmentVariable('DEMO_AUTH_SIGNING_KEY', '')

param logAnalyticsDailyQuotaGb = 1
