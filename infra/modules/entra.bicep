// Entra ID app registrations (Microsoft Graph Bicep extension):
//   - API: exposes the delegated scopes and the Files.Sender app role validated by the Functions backend.
//   - SPA: the Angular frontend (MSAL, auth code + PKCE), pre-authorized on the API (no consent prompt).
//   - OneDrive import: public client for personal Microsoft accounts (delegated Files.ReadWrite on Graph).
// The deployment identity needs the Graph application permission Application.ReadWrite.OwnedBy
// (granted by infra/bootstrap): it becomes owner of the apps it creates.
extension microsoftGraphV1

param appName string
param environmentName string

@description('SPA redirect URIs (origins, no trailing slash: MSAL uses window.location.origin).')
param spaRedirectUris array

var apiUniqueName = '${appName}-api-${environmentName}'
var spaUniqueName = '${appName}-spa-${environmentName}'

// Azure CLI public client: pre-authorized for execute_script (import scripts in tools/scripts use `az account get-access-token`).
var azureCliAppId = '04b07795-8ddb-461a-bbee-02f9e1bf7b46'

var scopeIds = {
  accessAsUser: guid(apiUniqueName, 'scope', 'access_as_user')
  executeScript: guid(apiUniqueName, 'scope', 'execute_script')
}
var filesSenderRoleId = guid(apiUniqueName, 'approle', 'Files.Sender')

// Policy-compliant identifier URI that does not need the appId (not known before creation).
var apiIdentifierUri = 'api://${tenant().tenantId}/${apiUniqueName}'

resource spaApp 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: spaUniqueName
  displayName: 'ExpenseTracker Web (${environmentName})'
  signInAudience: 'AzureADMyOrg'
  spa: {
    redirectUris: spaRedirectUris
  }
}

resource spaServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: spaApp.appId
}

resource apiApp 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: apiUniqueName
  displayName: 'ExpenseTracker API (${environmentName})'
  signInAudience: 'AzureADMyOrg'
  identifierUris: [
    apiIdentifierUri
  ]
  api: {
    // v2 tokens: aud = API client ID, accepted by JwtTokenValidator.
    requestedAccessTokenVersion: 2
    oauth2PermissionScopes: [
      {
        id: scopeIds.accessAsUser
        value: 'access_as_user'
        type: 'User'
        isEnabled: true
        adminConsentDisplayName: 'Access ExpenseTracker'
        adminConsentDescription: 'Allows the app to access ExpenseTracker on behalf of the signed-in user.'
        userConsentDisplayName: 'Access ExpenseTracker'
        userConsentDescription: 'Allows the app to access ExpenseTracker on your behalf.'
      }
      {
        id: scopeIds.executeScript
        value: 'execute_script'
        type: 'User'
        isEnabled: true
        adminConsentDisplayName: 'Run ExpenseTracker scripts'
        adminConsentDescription: 'Allows scripts to call ExpenseTracker (imports) on behalf of the signed-in user.'
        userConsentDisplayName: 'Run ExpenseTracker scripts'
        userConsentDescription: 'Allows scripts to call ExpenseTracker on your behalf.'
      }
    ]
    preAuthorizedApplications: [
      {
        appId: spaApp.appId
        delegatedPermissionIds: [
          scopeIds.accessAsUser
        ]
      }
      {
        appId: azureCliAppId
        delegatedPermissionIds: [
          scopeIds.executeScript
        ]
      }
    ]
  }
  appRoles: [
    {
      id: filesSenderRoleId
      value: 'Files.Sender'
      displayName: 'Files.Sender'
      description: 'Can upload bank files to the import endpoints.'
      allowedMemberTypes: [
        'User'
        'Application'
      ]
      isEnabled: true
    }
  ]
}

resource apiServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: apiApp.appId
}

// OneDrive import (functions OneDrive_*): the watched folders are on the personal OneDrive (Microsoft account)
// of each user, where Microsoft Graph does not allow app-only access. The Function App therefore uses delegated
// refresh tokens, obtained when a user connects OneDrive from the app (device code flow) and kept in Key Vault.
// Public client (no secret), personal Microsoft accounts only.
var oneDriveUniqueName = '${appName}-onedrive-${environmentName}'
var graphAppId = '00000003-0000-0000-c000-000000000000'
var graphDelegatedScopeIds = {
  filesReadWrite: '5c28f0bf-8a70-41f1-8ab2-9032436ddb65'
  offlineAccess: '7427e0e9-2fba-42fe-b0c0-848c9e6a8182'
}

resource oneDriveApp 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: oneDriveUniqueName
  displayName: 'ExpenseTracker OneDrive import (${environmentName})'
  signInAudience: 'PersonalMicrosoftAccount'
  isFallbackPublicClient: true // device code flow + refresh without client secret
  api: {
    requestedAccessTokenVersion: 2 // required for personal Microsoft accounts
  }
  publicClient: {
    redirectUris: [
      '${environment().authentication.loginEndpoint}common/oauth2/nativeclient'
    ]
  }
  requiredResourceAccess: [
    {
      resourceAppId: graphAppId
      resourceAccess: [
        {
          id: graphDelegatedScopeIds.filesReadWrite
          type: 'Scope'
        }
        {
          id: graphDelegatedScopeIds.offlineAccess
          type: 'Scope'
        }
      ]
    }
  ]
}

output apiClientId string = apiApp.appId
output apiIdentifierUri string = apiIdentifierUri
output apiScope string = '${apiIdentifierUri}/access_as_user'
output apiServicePrincipalId string = apiServicePrincipal.id
output filesSenderRoleId string = filesSenderRoleId
output spaClientId string = spaApp.appId
output spaServicePrincipalId string = spaServicePrincipal.id
output oneDriveClientId string = oneDriveApp.appId
