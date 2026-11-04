# 13 - Infrastructure as Code (Tecnico)

## Struttura
L'ambiente è descritto in Bicep nella cartella `infra/`: risorse Azure e app registration Entra ID (estensione **Microsoft Graph Bicep** `microsoftGraphV1`, registrata in `infra/bicepconfig.json` come `br:mcr.microsoft.com/bicep/extensions/microsoftgraph/v1.0:1.0.0`, richiede Bicep ≥ 0.36.1). Gli unici input esterni sono il nome del resource group (creato dal bootstrap se non esiste), SQL server e database già esistenti (più il repository GitHub per l'OIDC); tenant e subscription sono ricavati dal contesto (`tenant()`, `subscription()`).

| File | Risorse |
|---|---|
| `infra/main.bicep` | Entry point (scope **resource group**): naming, tag (`application`, `environment`, `managedBy=bicep`), orchestrazione moduli, output |
| `modules/entra.bicep` | App registration **API** e **SPA** + service principal, app registration **OneDrive import** (vedi sotto) |
| `modules/monitoring.bicep` | Log Analytics (`PerGB2018`, retention 30 gg, daily cap) + Application Insights workspace-based con local auth disabilitata (`DisableLocalAuth`: telemetria solo con identità Entra ID) |
| `modules/storage.bicep` | Storage `StorageV2` LRS con shared key **disabilitata**, container `app-package` per il pacchetto Flex |
| `modules/functionApp.bicep` | Piano `FC1` (Flex Consumption, Linux) + Function App `dotnet-isolated` 10.0, identità system-assigned, CORS, app settings, policy basic auth, role assignment |
| `main.bicep` (risorse `existing` `sqlServer` / `sqlDatabase`) | SQL server e database **già esistenti** (non creati né modificati), referenziati direttamente con scope sul resource group del server (`sqlServerResourceGroup`, default il resource group del deploy): forniscono FQDN e connection string. Niente modulo: un modulo sarebbe un deployment annidato in quel resource group, dove l'identità della pipeline ha solo `Reader` (manca `Microsoft.Resources/deployments/write`) |
| `modules/keyVault.bicep` | Key Vault con RBAC, soft delete 7 gg, segreto `DemoAuthSigningKey` (creato solo se la chiave è passata). I segreti `OneDriveRefreshToken` e `OneDriveClientState` **non** sono nel template: li scrive `scripts/connect-onedrive.ps1` |
| `modules/staticWebApp.bicep` | Static Web App `Free` per il frontend (non collegata al repo: il contenuto è caricato da `deploy-frontend.yml`) + dominio custom apex opzionale (`customDomainName`, validazione `dns-txt-token`) |
| `parameters/prod.bicepparam` | Parametri di produzione; nomi SQL e segreti letti con `readEnvironmentVariable()` |
| `bootstrap/main.bicep` + `bootstrap.bicepparam` | Bootstrap una tantum (scope **subscription**, eseguito in locale da un admin, vedi sotto) |
| `bootstrap/modules/resourceGroupRoles.bicep` | RBAC dell'identità di deploy su un resource group |
| `bootstrap/modules/filesSenderAssignments.bicep` | Assegnazione dell'app role `Files.Sender` agli utenti per UPN |
| `scripts/grant-function-sql-access.sql` | Crea l'utente DB della managed identity (`db_datareader` + `db_datawriter`) |
| `scripts/connect-onedrive.ps1` | Consenso una tantum per l'import da OneDrive (vedi sotto) |
| `.env.example` / `.env` | Modello committato / valori locali **gitignored** (`infra/.gitignore`) delle variabili d'ambiente (stessi nomi delle GitHub variables/secrets, più quelle del bootstrap) |
| `scripts/load-env.ps1` / `load-env.sh` | Caricano `infra/.env` (o `-Path` / primo argomento) nella sessione: righe `KEY=VALUE`, commenti `#`, virgolette rimosse; i valori vuoti rimuovono la variabile così valgono i default di `readEnvironmentVariable()`; avviso sulle obbligatorie vuote. `-SetSubscription` (solo PowerShell) esegue `az account set` con `AZURE_SUBSCRIPTION_ID` |

Naming di default: `<prefisso>-<appName>-<environmentName>[-<uniqueString>]` (es. `func-expensetrk-prod-xxxxxx`). Ogni nome Azure è sovrascrivibile da parametro (`functionAppName`, `storageAccountName`, `keyVaultName`, `staticWebAppName`) per adottare risorse esistenti.

SQL server e database non seguono il naming: i nomi arrivano dalle variabili d'ambiente `SQL_SERVER_NAME`, `SQL_DATABASE_NAME` e (opzionale) `SQL_SERVER_RESOURCE_GROUP`. Amministratore Entra ID, firewall (serve `AllowAllWindowsAzureIps`: Flex Consumption non ha IP in uscita fissi), SKU e backup restano gestiti fuori da Bicep. Se il server non esiste il deploy fallisce.

## App registration (`modules/entra.bicep`)
Identificate da `uniqueName` (upsert idempotente). API e SPA sono single-tenant (`AzureADMyOrg`):

- **API** (`<appName>-api-<env>`): identifier URI `api://<tenant-id>/<appName>-api-<env>` (formato ammesso dalla policy Entra senza conoscere l'appId prima della creazione); `requestedAccessTokenVersion: 2` → `aud` = client ID, accettato da `JwtTokenValidator`. Scope delegati `access_as_user` (frontend) ed `execute_script` (script di import); `preAuthorizedApplications`: SPA per `access_as_user`, Azure CLI (`04b07795-8ddb-461a-bbee-02f9e1bf7b46`) per `execute_script`. App role `Files.Sender` (`User` + `Application`), richiesto da `ImportFunctions`. ID di scope e ruolo deterministici (`guid()`).
- **SPA** (`<appName>-spa-<env>`): piattaforma `spa` con redirect URI = origine della Static Web App + `extraSpaRedirectUris` (default `http://localhost:5173`); MSAL usa `window.location.origin`, quindi senza slash finale. Gli ambienti di preview delle PR della Static Web App hanno hostname diversi e non sono registrati.

Le app settings `AzureAd__TenantId` / `AzureAd__ClientId` / `AzureAd__Audience` sono valorizzate con `tenant().tenantId`, client ID e identifier URI dell'API. L'utente è identificato dal claim `oid` (`CurrentUserAccessor`), indipendente dall'app registration: il passaggio a nuove app non cambia l'associazione dei dati.

- **OneDrive import** (`<appName>-onedrive-<env>`): `signInAudience: PersonalMicrosoftAccount` (la cartella è su un OneDrive personale, dove Graph non ammette accesso app-only), client pubblico (`isFallbackPublicClient: true`, redirect `…/common/oauth2/nativeclient`), `requestedAccessTokenVersion: 2` (obbligatorio per gli account personali), permessi delegati Graph `Files.ReadWrite` + `offline_access`. Nessun service principal e nessun secret: il consenso è dato dal proprietario del OneDrive con `connect-onedrive.ps1`.

Output per il frontend e gli script: `tenantId`, `spaClientId`, `apiClientId`, `apiIdentifierUri`, `apiScope`, `apiServicePrincipalId`, `filesSenderRoleId`, `oneDriveClientId`.

## Bootstrap (`infra/bootstrap`)
Eseguito in locale (`az deployment sub create -l $env:AZURE_LOCATION --parameters infra/bootstrap/bootstrap.bicepparam`) da un utente con Owner sulla subscription (o sui resource group) e *Privileged Role Administrator* (o Global Admin) in Entra ID; rieseguibile.

- Resource group dell'app (`AZURE_RESOURCE_GROUP`, location `AZURE_LOCATION`): creato se non esiste (la pipeline ha permessi solo al suo interno e non può crearlo).
- App registration + service principal `expensetrk-github-deploy` con federated credentials GitHub (`repo:<GITHUB_REPOSITORY>:environment:production` e `repo:<GITHUB_REPOSITORY>:pull_request`; quest'ultima resta per compatibilità, oggi tutti i job della pipeline usano l'environment).
- Permesso applicativo Graph `Application.ReadWrite.OwnedBy` (app role assignment sul service principal di Microsoft Graph, ID risolto per nome): la pipeline crea e aggiorna solo le app registration di cui è owner.
- RBAC sul resource group dell'app: `Contributor` + `Role Based Access Control Administrator` con **condizione ABAC** che consente solo di assegnare/rimuovere `Storage Blob Data Owner`, `Storage Queue Data Contributor`, `Key Vault Secrets User`, `Key Vault Secrets Officer` e `Monitoring Metrics Publisher` (i ruoli che `functionApp.bicep` assegna alla managed identity). Aggiungendo un ruolo al template va aggiunto anche qui e rieseguito il bootstrap, prima del deploy. `Reader` sul resource group del SQL server se diverso.
- Opzionale (`FILES_SENDER_ASSIGN=true`, utenti in `FILES_SENDER_UPNS` separati da virgola): assegnazione dell'app role `Files.Sender` dell'API agli utenti. Va eseguito dopo il primo deploy di `main.bicep` (l'API deve esistere); richiede privilegi (`AppRoleAssignment.ReadWrite.All`) volutamente non concessi alla CI.
- Output: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` da copiare nei secrets GitHub.

`infra/scripts/bootstrap.ps1` automatizza tutto: carica `infra/.env`, esegue il deploy di bootstrap e, con la GitHub CLI, crea l'environment `production` e ne imposta secret (output + `DEMO_AUTH_SIGNING_KEY`) e variabili (da `.env`, quelle vuote sono saltate). Idempotente, supporta `-WhatIf`, `-SkipBootstrap` e `-SkipGitHub`.

## Niente più Logic App
Entrambi i flussi che giravano su Logic App sono ora function della Function App (vedi modulo 03):
- **Reset demo** → timer function `Scheduled_DemoDataReset`, orario dal parametro `demoResetSchedule` → app setting `DemoResetSchedule`.
- **Import da OneDrive** → `OneDrive_Notifications` / `OneDrive_Sync` / `OneDrive_SubscriptionRenewal`: notifiche Graph in tempo reale invece del controllo giornaliero (vedi modulo 08). Parametri `oneDriveFolderId` (in `prod.bicepparam`), `oneDriveImportEnabled` (`ONEDRIVE_IMPORT_ENABLED`, default `false`), `oneDriveTargetUserEmail` (`ONEDRIVE_TARGET_USER_EMAIL`), `oneDriveSubscriptionRenewalSchedule` (default `0 0 4 * * *`).

Il deploy incrementale non rimuove le risorse tolte dal template: dopo il primo deploy vanno eliminate a mano la Logic App di import, la sua connessione OneDrive e l'identità user-assigned (e, se ancora presente, la vecchia Logic App di reset demo):

```powershell
az logic workflow delete -g <rg> -n logic-<appName>-import-<env> --yes
az resource delete -g <rg> -n onedrive --resource-type Microsoft.Web/connections
az identity delete -g <rg> -n id-<appName>-logic-<env>
az logic workflow delete -g <rg> -n logic-<appName>-demo-reset-<env> --yes
```

L'eventuale associazione `Users.AppOid` all'identità della Logic App può restare: non è più usata.

### Consenso OneDrive (`scripts/connect-onedrive.ps1`)
Una tantum, dopo un deploy con `ONEDRIVE_IMPORT_ENABLED=true` (e di nuovo solo se il refresh token viene revocato o resta inutilizzato per 90 giorni):
1. Legge `oneDriveClientId`, `keyVaultName` e `functionAppName` dagli output dell'ultimo deployment riuscito del resource group (sovrascrivibili da parametro).
2. Device code flow su `login.microsoftonline.com/consumers` (scope `Files.ReadWrite offline_access`) con l'account Microsoft proprietario del OneDrive; verifica con `GET /me/drive`.
3. Scrive in Key Vault `OneDriveRefreshToken` e, se manca, un `OneDriveClientState` casuale di 32 byte (se esiste viene mantenuto: la subscription attiva è stata creata con quel valore e i rinnovi non lo cambiano), passando i valori da file temporaneo (`az keyvault secret set -f`). Chi lo esegue deve avere `Key Vault Secrets Officer` sul vault.
4. Riavvia la Function App, che così ricarica il riferimento Key Vault del clientState, e lancia `OneDrive_SubscriptionRenewal` (`POST /admin/functions/...` con la master key). La function crea la baseline della delta e la subscription Graph. `-SkipSubscription` salta questo passo.

## App settings della Function App (gestite da Bicep)
- `AzureWebJobsStorage__accountName` — connessione identity-based allo storage (ruoli `Storage Blob Data Owner` e `Storage Queue Data Contributor`, quest'ultimo per la coda `onedrive-sync`).
- `APPLICATIONINSIGHTS_CONNECTION_STRING` + `APPLICATIONINSIGHTS_AUTHENTICATION_STRING=Authorization=AAD` (ruolo `Monitoring Metrics Publisher` sull'Application Insights), `AZURE_FUNCTIONS_ENVIRONMENT=Production`.
- `AzureAd__TenantId`, `AzureAd__ClientId`, `AzureAd__Audience` — dall'app registration API.
- `ConnectionStrings__DefaultConnection` — passwordless, `Authentication=Active Directory Default` (managed identity).
- `App__FEUrl` = dominio custom se impostato, altrimenti URL della Static Web App; la CORS di piattaforma include tutte le origini del frontend più `extraCorsOrigins` (in `prod.bicepparam`: `https://portal.azure.com`, per il test delle funzioni dal portale). `supportCredentials: false` (autenticazione con bearer token, nessun cookie).
- `DemoAuth__Enabled`; se attivo, `DemoAuth__SigningKey` è un riferimento Key Vault (`@Microsoft.KeyVault(SecretUri=...)`, ruolo `Key Vault Secrets User`).
- `DemoResetSchedule` — espressione NCRONTAB (UTC: su Linux/Flex `WEBSITE_TIME_ZONE` non è supportato) della timer function `Scheduled_DemoDataReset`, dal parametro `demoResetSchedule` (default `0 0 3 * * *`, ogni giorno alle 03:00 UTC). È risolta dall'host Functions (`%DemoResetSchedule%`), quindi deve essere un app setting e non una chiave di `appsettings.json`. I lock del timer usano lo storage dell'host (`Storage Blob Data Owner`, già assegnato).
- `OneDriveImport__Enabled`, `OneDriveImport__ClientId` (app registration OneDrive), `OneDriveImport__FolderId`, `OneDriveImport__TargetUserEmail`, `OneDriveImport__KeyVaultUri` e, solo se l'import è attivo, `OneDriveImport__ClientState` come riferimento Key Vault al segreto `OneDriveClientState`. La function legge e riscrive il refresh token direttamente con l'SDK Key Vault, perché il token ruota a ogni rinnovo; per questo ha anche il ruolo `Key Vault Secrets Officer`. `OneDriveImport__NotificationUrl` non è impostato: la function lo ricava da `WEBSITE_HOSTNAME`.
- `OneDriveSubscriptionRenewalSchedule`: espressione NCRONTAB (UTC) del timer `OneDrive_SubscriptionRenewal`, come `DemoResetSchedule`.
- `FUNCTIONS_WORKER_RUNTIME` **non** è impostato: su Flex Consumption il runtime è in `functionAppConfig.runtime`.

Policy `basicPublishingCredentialsPolicies`: `scm` = `enableScmBasicAuth` (default `false`: `deploy-backend.yml` pubblica con l'identità OIDC via RBAC, senza publish profile), `ftp` = `false`.

## Pipeline (`.github/workflows/deploy-infra.yml`)
Trigger: `push` su `main` e `pull_request` verso `main` sui path `infra/**` o il workflow stesso, più `workflow_dispatch`. Concurrency per PR / `main`, senza cancellare deploy in corso. **Tutti i job** referenziano l'environment GitHub `production`, dove stanno variabili e secret (le variabili/secret di environment sono visibili solo ai job che lo referenziano); il token OIDC ha quindi sempre subject `environment:production`. Eventuali *required reviewers* sull'environment bloccano anche il what-if delle PR.

1. **validate** — controlla le variabili obbligatorie, `az bicep build` + `az bicep lint` del template, `az bicep build-params`.
2. **what-if** — login OIDC (`azure/login@v2`), `az deployment group what-if` con l'output nel job summary. Le risorse Microsoft Graph non sono valutate dal what-if.
3. **deploy** — solo su push/dispatch, environment GitHub `production`: `az deployment group create --parameters infra/parameters/prod.bicepparam` (il template è risolto dal `using`); output e valori `VITE_*` per il frontend nel job summary.. Il resource group è creato dal bootstrap.

Secret e variabili dell'environment `production`:

| Secret | Obbligatorio | Uso |
|---|---|---|
| `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_SUBSCRIPTION_ID` | Sì | Login OIDC dell'identità di deploy (output del bootstrap) |
| `DEMO_AUTH_SIGNING_KEY` | No | Scritto in Key Vault quando valorizzato (basta una volta) |

| Variabile | Obbligatoria | Uso |
|---|---|---|
| `AZURE_RESOURCE_GROUP` | Sì | Resource group di destinazione (creato dal bootstrap) |
| `SQL_SERVER_NAME`, `SQL_DATABASE_NAME` | Sì | Nomi del SQL server e del database esistenti |
| `SQL_SERVER_RESOURCE_GROUP` | No | Resource group del SQL server, se diverso da `AZURE_RESOURCE_GROUP` |
| `DEMO_AUTH_ENABLED` | No | `true` per attivare il login demo (default `false`) |
| `CUSTOM_DOMAIN` | No | Dominio custom apex della Static Web App (es. `expensetracker.lol`); da impostare solo dopo averlo rimosso dal sito precedente |
| `ONEDRIVE_IMPORT_ENABLED` | No | `true` per attivare l'import automatico da OneDrive (default `false`); poi eseguire `connect-onedrive.ps1` |
| `ONEDRIVE_TARGET_USER_EMAIL` | Se l'import è attivo | Email dell'utente ExpenseTracker proprietario dei dati importati |

## Passi manuali residui
- Bootstrap (una tantum, privilegiato): crea l'identità con cui la pipeline stessa si autentica.
- Consenso OneDrive con `infra/scripts/connect-onedrive.ps1` (device code, interattivo, non automatizzabile).
- Eliminazione delle risorse della vecchia Logic App di import (sopra).
- DNS del dominio custom: record TXT con `customDomainValidationToken` e ALIAS/A verso `staticWebAppDefaultHostname`.
- Utente DB della managed identity: `infra/scripts/grant-function-sql-access.sql` (T-SQL sul data plane, non esprimibile in Bicep).
- Migrazioni EF: da macchina locale con l'admin Entra ID (IP del client nel firewall del server esistente).
- Script di import in `tools/scripts`: `-Audience <apiIdentifierUri>`. Le pipeline applicative non richiedono configurazione: `deploy-backend.yml` e `deploy-frontend.yml` usano l'environment `production` e leggono dagli output dell'ultimo deploy infra (`infra-*`) Function App, valori `VITE_*` e Static Web App (deployment token letto a runtime).
