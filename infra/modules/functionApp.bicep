// Function App on the Flex Consumption plan (Linux, dotnet-isolated .NET 10) with system-assigned identity.
// The identity is used for AzureWebJobsStorage (blobs + the onedrive-sync queue), the deployment container,
// Key Vault (references + the OneDrive refresh token, rewritten at every rotation) and Azure SQL.
param location string
param tags object
param planName string
param functionAppName string
param storageAccountName string
param deploymentContainerName string
param appInsightsName string
param appInsightsConnectionString string
param keyVaultName string
param demoAuthSecretUri string
param demoAuthEnabled bool

@description('NCRONTAB schedule (UTC) of Scheduled_DemoDataReset (Linux/Flex ignores WEBSITE_TIME_ZONE).')
param demoResetSchedule string

@description('Key Vault URI (https://<name>.vault.azure.net/): the OneDrive refresh token is read/written with the SDK.')
param keyVaultUri string

@description('Enable the OneDrive import functions (OneDrive_Notifications / OneDrive_Sync / OneDrive_SubscriptionRenewal).')
param oneDriveImportEnabled bool

@description('Client ID of the OneDrive app registration (personal Microsoft accounts, public client).')
param oneDriveClientId string

@description('OneDrive item ID of the watched folder.')
param oneDriveFolderId string

@description('Email of the ExpenseTracker user that owns the imported data.')
param oneDriveTargetUserEmail string

@description('Versionless URI of the OneDriveClientState secret (written by infra/scripts/connect-onedrive.ps1).')
param oneDriveClientStateSecretUri string

@description('NCRONTAB schedule (UTC) of OneDrive_SubscriptionRenewal.')
param oneDriveSubscriptionRenewalSchedule string
param sqlConnectionString string
param azureAdTenantId string
param azureAdClientId string
param azureAdAudience string
param frontendUrl string
param corsAllowedOrigins array
param enableScmBasicAuth bool
param maximumInstanceCount int

@allowed([
  512
  2048
  4096
])
param instanceMemoryMB int = 2048

var roleIds = {
  storageBlobDataOwner: 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
  storageQueueDataContributor: '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
  keyVaultSecretsUser: '4633458b-17de-408a-b874-0445c86b69e6'
  keyVaultSecretsOfficer: 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7'
  monitoringMetricsPublisher: '3913510d-42f4-4e42-8a64-420c390055eb'
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: planName
  location: location
  tags: tags
  kind: 'functionapp'
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  properties: {
    reserved: true // Linux
  }
}

var baseAppSettings = [
  // Identity-based connection to the host storage (no account key).
  {
    name: 'AzureWebJobsStorage__accountName'
    value: storage.name
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: appInsightsConnectionString
  }
  // Application Insights has local auth disabled: telemetry is sent with the system-assigned identity.
  {
    name: 'APPLICATIONINSIGHTS_AUTHENTICATION_STRING'
    value: 'Authorization=AAD'
  }
  {
    name: 'AZURE_FUNCTIONS_ENVIRONMENT'
    value: 'Production'
  }
  {
    name: 'AzureAd__TenantId'
    value: azureAdTenantId
  }
  {
    name: 'AzureAd__ClientId'
    value: azureAdClientId
  }
  {
    name: 'AzureAd__Audience'
    value: azureAdAudience
  }
  {
    name: 'ConnectionStrings__DefaultConnection'
    value: sqlConnectionString
  }
  {
    name: 'App__FEUrl'
    value: frontendUrl
  }
  {
    name: 'DemoAuth__Enabled'
    value: string(demoAuthEnabled)
  }
  // Resolved by the Functions host for the %DemoResetSchedule% timer expression.
  {
    name: 'DemoResetSchedule'
    value: demoResetSchedule
  }
  // Import automatico da OneDrive. NotificationUrl vuoto: la function usa WEBSITE_HOSTNAME.
  {
    name: 'OneDriveImport__Enabled'
    value: string(oneDriveImportEnabled)
  }
  {
    name: 'OneDriveImport__ClientId'
    value: oneDriveClientId
  }
  {
    name: 'OneDriveImport__FolderId'
    value: oneDriveFolderId
  }
  {
    name: 'OneDriveImport__TargetUserEmail'
    value: oneDriveTargetUserEmail
  }
  {
    name: 'OneDriveImport__KeyVaultUri'
    value: keyVaultUri
  }
  {
    name: 'OneDriveSubscriptionRenewalSchedule'
    value: oneDriveSubscriptionRenewalSchedule
  }
]

// The secret exists only after connect-onedrive.ps1: the reference is added only when the import is enabled.
var oneDriveAppSettings = oneDriveImportEnabled
  ? [
      {
        name: 'OneDriveImport__ClientState'
        value: '@Microsoft.KeyVault(SecretUri=${oneDriveClientStateSecretUri})'
      }
    ]
  : []

var demoAuthAppSettings = demoAuthEnabled
  ? [
      {
        name: 'DemoAuth__SigningKey'
        value: '@Microsoft.KeyVault(SecretUri=${demoAuthSecretUri})'
      }
    ]
  : []

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: functionAppName
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      http20Enabled: true
      cors: {
        allowedOrigins: corsAllowedOrigins
        supportCredentials: false
      }
      appSettings: concat(baseAppSettings, demoAuthAppSettings, oneDriveAppSettings)
    }
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storage.properties.primaryEndpoints.blob}${deploymentContainerName}'
          authentication: {
            type: 'SystemAssignedIdentity'
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: maximumInstanceCount
        instanceMemoryMB: instanceMemoryMB
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
    }
  }
}

// Publish-profile deploys (deploy-backend.yml) need SCM basic auth; disable it once the
// backend pipeline moves to OIDC.
resource scmBasicAuth 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2024-04-01' = {
  parent: functionApp
  name: 'scm'
  properties: {
    allow: enableScmBasicAuth
  }
}

resource ftpBasicAuth 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2024-04-01' = {
  parent: functionApp
  name: 'ftp'
  properties: {
    allow: false
  }
}

// Host storage (blobs: leases, keys, timers) + deployment package container.
resource storageBlobOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storage
  name: guid(storage.id, functionApp.id, roleIds.storageBlobDataOwner)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleIds.storageBlobDataOwner)
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Queue trigger/output of the OneDrive import (queue onedrive-sync in the host storage).
resource storageQueueContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storage
  name: guid(storage.id, functionApp.id, roleIds.storageQueueDataContributor)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleIds.storageQueueDataContributor)
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource keyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, functionApp.id, roleIds.keyVaultSecretsUser)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleIds.keyVaultSecretsUser)
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// The OneDrive refresh token rotates at every renewal and is written back to Key Vault.
resource keyVaultSecretsOfficer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, functionApp.id, roleIds.keyVaultSecretsOfficer)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleIds.keyVaultSecretsOfficer)
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource appInsightsPublisher 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: appInsights
  name: guid(appInsights.id, functionApp.id, roleIds.monitoringMetricsPublisher)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleIds.monitoringMetricsPublisher)
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output name string = functionApp.name
output defaultHostname string = functionApp.properties.defaultHostName
output principalId string = functionApp.identity.principalId
