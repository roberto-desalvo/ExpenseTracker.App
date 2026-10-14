$managedIdentityObjectId = ""
$servicePrincipalObjectId = "" # object id dell'app registration, da vedere su Entra Id
$appRoleId = "" # lo vedi sempre da Entra Id

# Crea un file json temporaneo
@"
{
  "principalId": "$managedIdentityObjectId",
  "resourceId": "$servicePrincipalObjectId",
  "appRoleId": "$appRoleId"
}
"@ | Out-File -FilePath body.json -Encoding utf8

# Esegui il comando puntando al file
az rest --method POST `
  --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$servicePrincipalObjectId/appRoleAssignedTo" `
  --headers "Content-Type=application/json" `
  --body "@body.json"

