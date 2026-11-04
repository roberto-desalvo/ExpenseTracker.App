# 13 - Infrastructure as Code (Tecnico)

## Struttura
L'infrastruttura Azure è descritta in Bicep nella cartella `infra/` (scope **resource group**):

| File | Risorse |
|---|---|
| `infra/main.bicep` | Entry point: naming, tag (`application`, `environment`, `managedBy=bicep`), orchestrazione moduli, output |
| `modules/monitoring.bicep` | Log Analytics (`PerGB2018`, retention 30 gg, daily cap) + Application Insights workspace-based |
| `modules/storage.bicep` | Storage `StorageV2` LRS con shared key **disabilitata**, container `app-package` per il pacchetto Flex |
| `modules/functionApp.bicep` | Piano `FC1` (Flex Consumption, Linux) + Function App `dotnet-isolated` 10.0, identità system-assigned, CORS, app settings, policy basic auth, role assignment |
| `modules/sql.bicep` | Riferimento `existing` a SQL server e database **già esistenti** (non creati né modificati): fornisce FQDN e connection string. Deployato con scope sul resource group del server (`sqlServerResourceGroup`, default il resource group del deploy) |
| `modules/keyVault.bicep` | Key Vault con RBAC, soft delete 7 gg, segreto `DemoAuthSigningKey` (creato solo se la chiave è passata) |
| `modules/staticWebApp.bicep` | Static Web App `Free` per il frontend (non collegata al repo: il contenuto è caricato da `deploy-frontend.yml`) |
| `parameters/prod.bicepparam` | Parametri di produzione; i valori per tenant/segreti sono letti con `readEnvironmentVariable()` |
| `scripts/grant-function-sql-access.sql` | Crea l'utente DB della managed identity (`db_datareader` + `db_datawriter`) |
| `.env.example` / `.env` | Modello committato / valori locali **gitignored** (`infra/.gitignore`) delle variabili d'ambiente del deploy (stessi nomi delle GitHub variables/secrets, più `AZURE_SUBSCRIPTION_ID`) |
| `scripts/load-env.ps1` / `load-env.sh` | Caricano `infra/.env` (o `-Path` / primo argomento) nella sessione: righe `KEY=VALUE`, commenti `#`, virgolette rimosse; i valori vuoti rimuovono la variabile così valgono i default di `readEnvironmentVariable()`; avviso sulle obbligatorie vuote. `-SetSubscription` (solo PowerShell) esegue `az account set` con `AZURE_SUBSCRIPTION_ID` |

Naming di default: `<prefisso>-<appName>-<environmentName>[-<uniqueString>]` (es. `func-expensetrk-prod-xxxxxx`). Ogni nome è sovrascrivibile da parametro (`functionAppName`, `storageAccountName`, `keyVaultName`, `staticWebAppName`) per adottare risorse esistenti.

SQL server e database non seguono il naming: i nomi arrivano dalle variabili d'ambiente `SQL_SERVER_NAME`, `SQL_DATABASE_NAME` e (opzionale) `SQL_SERVER_RESOURCE_GROUP`, lette da `prod.bicepparam`. Amministratore Entra ID, firewall (serve `AllowAllWindowsAzureIps`: Flex Consumption non ha IP in uscita fissi), SKU e backup restano gestiti fuori da Bicep. Se il server non esiste il deploy fallisce.

## App settings della Function App (gestite da Bicep)
- `AzureWebJobsStorage__accountName` — connessione identity-based allo storage (ruolo `Storage Blob Data Owner`).
- `APPLICATIONINSIGHTS_CONNECTION_STRING`, `AZURE_FUNCTIONS_ENVIRONMENT=Production`.
- `AzureAd__TenantId`, `AzureAd__ClientId`, `AzureAd__Audience`.
- `ConnectionStrings__DefaultConnection` — passwordless, `Authentication=Active Directory Default` (managed identity).
- `App__FEUrl` = URL della Static Web App; la CORS di piattaforma include lo stesso URL più `extraCorsOrigins`.
- `DemoAuth__Enabled`; se attivo, `DemoAuth__SigningKey` è un riferimento Key Vault (`@Microsoft.KeyVault(SecretUri=...)`, ruolo `Key Vault Secrets User`).
- `FUNCTIONS_WORKER_RUNTIME` **non** è impostato: su Flex Consumption il runtime è in `functionAppConfig.runtime`.

Policy `basicPublishingCredentialsPolicies`: `scm` = `enableScmBasicAuth` (default `true`, necessario al deploy con publish profile di `deploy-backend.yml`), `ftp` = `false`.

## Pipeline (`.github/workflows/deploy-infra.yml`)
Trigger: `push` su `main` e `pull_request` verso `main` sui path `infra/**` o il workflow stesso, più `workflow_dispatch`. Concurrency per PR / `main`, senza cancellare deploy in corso.

1. **validate** — controlla le variabili obbligatorie, `az bicep build` + `az bicep lint` del template, `az bicep build-params`.
2. **what-if** — login OIDC (`azure/login@v2`), `az deployment group what-if` con l'output nel job summary (saltato se il resource group non esiste ancora).
3. **deploy** — solo su push/dispatch, environment GitHub `production`: crea il resource group se manca, `az deployment group create --parameters infra/parameters/prod.bicepparam` (il template è risolto dal `using`), output nel job summary.

| Secret | Obbligatorio | Uso |
|---|---|---|
| `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_SUBSCRIPTION_ID` | Sì | Login OIDC dell'identità di deploy |
| `DEMO_AUTH_SIGNING_KEY` | No | Scritto in Key Vault quando valorizzato (basta una volta) |

| Variabile | Obbligatoria | Uso |
|---|---|---|
| `AZURE_RESOURCE_GROUP` | Sì | Resource group di destinazione (condiviso con lo step CORS di `deploy-backend.yml`) |
| `AZURE_LOCATION` | No | Region del resource group se va creato (default `westeurope`) |
| `AZURE_AD_TENANT_ID`, `AZURE_AD_CLIENT_ID` | Sì | App settings `AzureAd__*` |
| `AZURE_AD_AUDIENCE` | No | App setting `AzureAd__Audience` |
| `SQL_SERVER_NAME`, `SQL_DATABASE_NAME` | Sì | Nomi del SQL server e del database esistenti |
| `SQL_SERVER_RESOURCE_GROUP` | No | Resource group del SQL server, se diverso da `AZURE_RESOURCE_GROUP` |
| `DEMO_AUTH_ENABLED` | No | `true` per attivare il login demo (default `false`) |

L'identità di deploy richiede federated credentials per `environment:production` e `pull_request`, e i ruoli `Contributor` + `Role Based Access Control Administrator` sul resource group (`Contributor` sulla subscription se il resource group va creato; `Reader` sul resource group del SQL server se è diverso).

## Passi manuali residui
- Utente DB della managed identity: `infra/scripts/grant-function-sql-access.sql` (T-SQL, non esprimibile in Bicep).
- Migrazioni EF: da macchina locale con l'admin Entra ID (IP del client nel firewall del server esistente).
- Allineamento pipeline applicative: `AZURE_FUNCTIONAPP_NAME` e publish profile per `deploy-backend.yml`; deployment token della Static Web App e `VITE_EXPENSE_TRACKER_API_BASE_URL` (output `functionAppUrl`) per `deploy-frontend.yml`.
