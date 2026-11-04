// Logic App that calls the backend with a user-assigned managed identity (app role Files.Sender on the API,
// assigned by infra/bootstrap):
//   - import: new files in a OneDrive folder -> /api/import/<bank>-csv/stream or -xlsx
// The daily demo data reset is the Scheduled_DemoDataReset timer function in the Function App.
// Workflow definitions live in infra/logicapps/*.json (exported from the designer) with __PLACEHOLDER__ tokens.
// The OneDrive API connection must be authorized once in the portal (OAuth consent cannot be automated).
param location string
param tags object
param identityName string
param importWorkflowName string
param oneDriveConnectionName string = 'onedrive'

@description('Backend base URL, without /api (e.g. https://<function-app>.azurewebsites.net).')
param backendBaseUrl string

@description('Audience of the tokens requested by the managed identity (API identifier URI).')
param apiAudience string

@description('OneDrive item ID of the watched folder (from the trigger metadata in the designer).')
param oneDriveFolderId string

@description('Display path of the watched folder (designer metadata only).')
param oneDriveFolderPath string

param logAnalyticsId string
param logAnalyticsName string

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

var oneDriveApiId = subscriptionResourceId('Microsoft.Web/locations/managedApis', location, 'onedrive')

// Redeploying keeps the existing OAuth authorization (no parameterValues are sent).
resource oneDriveConnection 'Microsoft.Web/connections@2016-06-01' = {
  name: oneDriveConnectionName
  location: location
  tags: tags
  #disable-next-line BCP187 // 'kind' is missing from the type definition but is set on Consumption connections
  kind: 'V1'
  properties: {
    displayName: 'OneDrive'
    api: {
      id: oneDriveApiId
    }
  }
}

func render(template string, backendBaseUrl string, apiAudience string, identityId string, folderId string, folderPath string) object =>
  json(replace(replace(replace(replace(replace(template,
    '__BACKEND_BASE_URL__', backendBaseUrl),
    '__API_AUDIENCE__', apiAudience),
    '__MANAGED_IDENTITY_ID__', identityId),
    '__ONEDRIVE_FOLDER_ID__', folderId),
    '__ONEDRIVE_FOLDER_PATH__', folderPath))

var importTemplate = loadTextContent('../logicapps/import-expenses-files.json')

var userAssignedIdentity = {
  type: 'UserAssigned'
  userAssignedIdentities: {
    '${identity.id}': {}
  }
}

resource importWorkflow 'Microsoft.Logic/workflows@2019-05-01' = {
  name: importWorkflowName
  location: location
  tags: tags
  identity: userAssignedIdentity
  properties: {
    state: 'Enabled'
    definition: render(importTemplate, backendBaseUrl, apiAudience, identity.id, oneDriveFolderId, oneDriveFolderPath)
    parameters: {
      '$connections': {
        value: {
          onedrive: {
            connectionId: oneDriveConnection.id
            connectionName: oneDriveConnection.name
            id: oneDriveApiId
          }
        }
      }
    }
  }
}

// Run history to Log Analytics, shown by the Logic Apps Management solution.
resource importDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'to-log-analytics'
  scope: importWorkflow
  properties: {
    workspaceId: logAnalyticsId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
  }
}

resource logicAppsManagement 'Microsoft.OperationsManagement/solutions@2015-11-01-preview' = {
  name: 'LogicAppsManagement(${logAnalyticsName})'
  location: location
  tags: tags
  plan: {
    name: 'LogicAppsManagement(${logAnalyticsName})'
    publisher: 'Microsoft'
    product: 'OMSGallery/LogicAppsManagement'
    promotionCode: ''
  }
  properties: {
    workspaceResourceId: logAnalyticsId
  }
}

output identityName string = identity.name
output identityPrincipalId string = identity.properties.principalId
output oneDriveConnectionName string = oneDriveConnection.name
output importWorkflowName string = importWorkflow.name
