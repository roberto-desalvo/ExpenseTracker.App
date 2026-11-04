// ExpenseTracker - one-time bootstrap (subscription scope), run LOCALLY by an admin, never by the pipeline:
//   - application resource group (created if missing)
//   - GitHub deployment identity: app registration + service principal + federated credentials (OIDC)
//   - Microsoft Graph permission Application.ReadWrite.OwnedBy for it (lets infra/main.bicep manage its app registrations)
//   - Azure RBAC: Contributor + constrained RBAC Administrator on the app resource group, Reader on the SQL resource group
//   - optional (assignFilesSender): Files.Sender app role of the API to users and to the Logic Apps identity
//     (both created by infra/main.bicep, so only after the first infra deploy)
// Requires: Owner (or User Access Administrator + Contributor) on the resource groups and the Entra role
// Privileged Role Administrator (or Global Administrator) to grant Graph application permissions.
targetScope = 'subscription'

extension microsoftGraphV1

@description('Resource group that hosts the application resources (created if missing).')
param resourceGroupName string

@description('Location of the application resource group (must match the existing one, if any).')
param location string = deployment().location

@description('Resource group of the existing SQL server (empty = resourceGroupName).')
param sqlServerResourceGroupName string = ''

@description('GitHub repository allowed to use the identity, as owner/name.')
param githubRepository string

@description('GitHub environment used by the deploy job.')
param githubEnvironment string = 'production'

@description('Unique name of the deployment app registration.')
param deployAppUniqueName string = 'expensetrk-github-deploy'

@description('Unique name of the API app registration created by infra/main.bicep (appName-api-environmentName).')
param apiAppUniqueName string = 'expensetrk-api-prod'

@description('Assign the API app role Files.Sender. Set to true only after the first infra deploy (API app and Logic Apps identity must exist).')
param assignFilesSender bool = false

@description('UPNs of the users to assign the API app role Files.Sender (import endpoints).')
param filesSenderUserPrincipalNames array = []

@description('Name of the Logic Apps managed identity created by infra/main.bicep (id-<appName>-logic-<environmentName>).')
param logicAppIdentityName string = 'id-expensetrk-logic-prod'

var graphAppId = '00000003-0000-0000-c000-000000000000'

resource deployApp 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: deployAppUniqueName
  displayName: 'ExpenseTracker GitHub deploy'
  signInAudience: 'AzureADMyOrg'
}

resource deploySp 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: deployApp.appId
}

// Deploy job (environment-scoped token) and what-if job on pull requests.
resource ficEnvironment 'Microsoft.Graph/applications/federatedIdentityCredentials@v1.0' = {
  name: '${deployApp.uniqueName}/github-env-${githubEnvironment}'
  audiences: [
    'api://AzureADTokenExchange'
  ]
  issuer: 'https://token.actions.githubusercontent.com'
  subject: 'repo:${githubRepository}:environment:${githubEnvironment}'
  description: 'GitHub Actions deploy job'
}

resource ficPullRequest 'Microsoft.Graph/applications/federatedIdentityCredentials@v1.0' = {
  name: '${deployApp.uniqueName}/github-pull-request'
  audiences: [
    'api://AzureADTokenExchange'
  ]
  issuer: 'https://token.actions.githubusercontent.com'
  subject: 'repo:${githubRepository}:pull_request'
  description: 'GitHub Actions what-if on pull requests'
  dependsOn: [
    ficEnvironment // federated credentials of the same app must not be written in parallel
  ]
}

resource graphSp 'Microsoft.Graph/servicePrincipals@v1.0' existing = {
  appId: graphAppId
}

// Create apps/service principals and manage only the ones it owns (admin consent of an application permission).
resource graphApplicationOwnedBy 'Microsoft.Graph/appRoleAssignedTo@v1.0' = {
  appRoleId: filter(graphSp.appRoles, role => role.value == 'Application.ReadWrite.OwnedBy')[0].id
  principalId: deploySp.id
  resourceId: graphSp.id
}

// The pipeline identity is scoped to this resource group and cannot create it: the bootstrap does.
resource appRg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
}

module appRgRoles 'modules/resourceGroupRoles.bicep' = {
  name: 'bootstrap-rbac-app'
  scope: appRg
  params: {
    principalId: deploySp.id
    grantContributor: true
  }
}

module sqlRgRoles 'modules/resourceGroupRoles.bicep' = if (!empty(sqlServerResourceGroupName) && sqlServerResourceGroupName != resourceGroupName) {
  name: 'bootstrap-rbac-sql'
  scope: resourceGroup(empty(sqlServerResourceGroupName) ? resourceGroupName : sqlServerResourceGroupName)
  params: {
    principalId: deploySp.id
    grantContributor: false
  }
}

module filesSender 'modules/filesSenderAssignments.bicep' = if (assignFilesSender) {
  name: 'bootstrap-files-sender'
  params: {
    apiAppUniqueName: apiAppUniqueName
    userPrincipalNames: filesSenderUserPrincipalNames
    resourceGroupName: appRg.name
    logicAppIdentityName: logicAppIdentityName
  }
}

// GitHub secrets for .github/workflows/deploy-infra.yml
output AZURE_CLIENT_ID string = deployApp.appId
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_SUBSCRIPTION_ID string = subscription().subscriptionId
