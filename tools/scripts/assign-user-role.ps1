
$userOid = "" # sempre da Entra Id
$servicePrincipalObjectId = "" # object id dell'app registration, da vedere su Entra Id
$appRoleId = "" # lo vedi sempre da Entra Id

@"
{
  "principalId": "$userOid",
  "resourceId": "$servicePrincipalObjectId",
  "appRoleId": "$appRoleId"
}
"@ | Out-File -FilePath body_user.json -Encoding utf8

az rest --method POST `
  --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$servicePrincipalObjectId/appRoleAssignedTo" `
  --headers "Content-Type=application/json" `
  --body "@body_user.json"