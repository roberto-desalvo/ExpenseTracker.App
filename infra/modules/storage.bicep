// Storage account for the Functions host (AzureWebJobsStorage), the Flex Consumption deployment package
// and the asynchronous imports (files uploaded from the app + their queue).
// Shared-key access is disabled: the Function App reaches it with its managed identity.
param location string
param tags object
param storageAccountName string

@description('Days after which the files uploaded for the asynchronous import are deleted.')
param importFilesRetentionDays int = 30

var deploymentContainerName = 'app-package'

// Must match ImportStorage:ContainerName (default) and the queue names in the Functions code.
var importContainerName = 'bank-imports'
var queueNames = [
  'onedrive-sync'
  'bank-file-import'
]

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    defaultToOAuthAuthentication: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource deploymentContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: deploymentContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource importContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: importContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource queues 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = [
  for queueName in queueNames: {
    parent: queueService
    name: queueName
  }
]

// The uploaded files are only needed while the import runs (and to inspect a failure): delete them after a while.
resource lifecyclePolicy 'Microsoft.Storage/storageAccounts/managementPolicies@2023-05-01' = {
  parent: storage
  name: 'default'
  properties: {
    policy: {
      rules: [
        {
          name: 'delete-import-files'
          enabled: true
          type: 'Lifecycle'
          definition: {
            filters: {
              blobTypes: [
                'blockBlob'
              ]
              prefixMatch: [
                '${importContainerName}/'
              ]
            }
            actions: {
              baseBlob: {
                delete: {
                  daysAfterModificationGreaterThan: importFilesRetentionDays
                }
              }
            }
          }
        }
      ]
    }
  }
  dependsOn: [
    importContainer
  ]
}

output name string = storage.name
output deploymentContainerName string = deploymentContainer.name
output importContainerName string = importContainer.name
