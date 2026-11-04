// Azure RBAC for the GitHub deployment identity on one resource group.
//   grantContributor = true  -> Contributor + RBAC Administrator limited (ABAC condition) to the roles
//                               that infra/main.bicep assigns to the Function App identity
//   grantContributor = false -> Reader (e.g. resource group of the existing SQL server)
param principalId string
param grantContributor bool

var roles = {
  contributor: 'b24988ac-6180-42a0-ab88-20f7382dd24c'
  rbacAdministrator: 'f58310d9-a9f6-439a-9e8d-f62e7b41a168'
  reader: 'acdd72a7-3385-48ef-bd42-f606fba81ae7'
  // Roles assigned by infra/modules/functionApp.bicep
  storageBlobDataOwner: 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
  storageQueueDataContributor: '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
  keyVaultSecretsUser: '4633458b-17de-408a-b874-0445c86b69e6'
  keyVaultSecretsOfficer: 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7'
  monitoringMetricsPublisher: '3913510d-42f4-4e42-8a64-420c390055eb'
}

var assignableRoles = '${roles.storageBlobDataOwner}, ${roles.storageQueueDataContributor}, ${roles.keyVaultSecretsUser}, ${roles.keyVaultSecretsOfficer}, ${roles.monitoringMetricsPublisher}'
var rbacCondition = '((!(ActionMatches{\'Microsoft.Authorization/roleAssignments/write\'})) OR (@Request[Microsoft.Authorization/roleAssignments:RoleDefinitionId] ForAnyOfAnyValues:GuidEquals {${assignableRoles}})) AND ((!(ActionMatches{\'Microsoft.Authorization/roleAssignments/delete\'})) OR (@Resource[Microsoft.Authorization/roleAssignments:RoleDefinitionId] ForAnyOfAnyValues:GuidEquals {${assignableRoles}}))'

resource contributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (grantContributor) {
  name: guid(resourceGroup().id, principalId, roles.contributor)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.contributor)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

resource rbacAdministrator 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (grantContributor) {
  name: guid(resourceGroup().id, principalId, roles.rbacAdministrator)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.rbacAdministrator)
    principalId: principalId
    principalType: 'ServicePrincipal'
    conditionVersion: '2.0'
    condition: rbacCondition
    description: 'Only the roles assigned by infra/main.bicep to the Function App identity'
  }
}

resource reader 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!grantContributor) {
  name: guid(resourceGroup().id, principalId, roles.reader)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.reader)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
