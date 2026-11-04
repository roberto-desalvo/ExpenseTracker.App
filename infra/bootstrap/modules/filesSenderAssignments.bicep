// Assigns the API app role Files.Sender (required by the import endpoints) to users (by UPN).
// The automatic OneDrive import runs inside the Function App and does not call the import endpoints.
// Kept out of the pipeline: assigning app roles requires AppRoleAssignment.ReadWrite.All, too powerful for CI.
targetScope = 'subscription'

extension microsoftGraphV1

param apiAppUniqueName string
param userPrincipalNames array

resource apiApp 'Microsoft.Graph/applications@v1.0' existing = {
  uniqueName: apiAppUniqueName
}

resource apiSp 'Microsoft.Graph/servicePrincipals@v1.0' existing = {
  appId: apiApp.appId
}

var filesSenderRoleId = filter(apiApp.appRoles, role => role.value == 'Files.Sender')[0].id

resource users 'Microsoft.Graph/users@v1.0' existing = [
  for upn in userPrincipalNames: {
    userPrincipalName: upn
  }
]

resource userAssignments 'Microsoft.Graph/appRoleAssignedTo@v1.0' = [
  for (upn, i) in userPrincipalNames: {
    appRoleId: filesSenderRoleId
    principalId: users[i].id
    resourceId: apiSp.id
  }
]
