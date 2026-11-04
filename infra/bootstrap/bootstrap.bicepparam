// Bootstrap parameters, read from infra/.env (load it with infra/scripts/load-env.ps1).
// az deployment sub create -l <location> -n expensetracker-bootstrap --parameters infra/bootstrap/bootstrap.bicepparam
using 'main.bicep'

param resourceGroupName = readEnvironmentVariable('AZURE_RESOURCE_GROUP')
param location = readEnvironmentVariable('AZURE_LOCATION', 'westeurope')
param sqlServerResourceGroupName = readEnvironmentVariable('SQL_SERVER_RESOURCE_GROUP', '')
param githubRepository = readEnvironmentVariable('GITHUB_REPOSITORY', 'roberto-desalvo/ExpenseTracker.App')
param githubEnvironment = 'production'

// Must match appName/environmentName of infra/parameters/prod.bicepparam.
param apiAppUniqueName = 'expensetrk-api-prod'

// Files.Sender assignments: FILES_SENDER_ASSIGN=true only after the first infra deploy.
param assignFilesSender = bool(readEnvironmentVariable('FILES_SENDER_ASSIGN', 'false'))
var filesSenderUpns = readEnvironmentVariable('FILES_SENDER_UPNS', '')
param filesSenderUserPrincipalNames = empty(filesSenderUpns) ? [] : map(split(filesSenderUpns, ','), upn => trim(upn))
