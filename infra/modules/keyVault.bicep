// Key Vault (RBAC authorization) holding application secrets referenced by the Function App settings.
param location string
param tags object
param keyVaultName string

@secure()
@description('Demo token signing key. Empty = do not create/update the secret.')
param demoAuthSigningKey string

var demoAuthSecretName = 'DemoAuthSigningKey'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
  }
}

resource demoAuthSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(demoAuthSigningKey)) {
  parent: keyVault
  name: demoAuthSecretName
  properties: {
    value: demoAuthSigningKey
    contentType: 'text/plain'
  }
}

output name string = keyVault.name
output vaultUri string = keyVault.properties.vaultUri
// Versionless URI: the Function App always resolves the latest version of the secret.
output demoAuthSecretUri string = '${keyVault.properties.vaultUri}secrets/${demoAuthSecretName}'
// Written by infra/scripts/connect-onedrive.ps1 (not by the template), together with OneDriveRefreshToken.
output oneDriveClientStateSecretUri string = '${keyVault.properties.vaultUri}secrets/OneDriveClientState'
