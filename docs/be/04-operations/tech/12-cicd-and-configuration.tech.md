# 12 - CI/CD and configuration (Tecnico)

## Workflow (`.github/workflows/deploy-backend.yml`)
`on: push` su `main` (path `api/**` o il workflow stesso) + `workflow_dispatch`, runner `ubuntu-latest`:

1. **build** — `actions/setup-dotnet@v4` (.NET 10.0.x) → `dotnet build` della solution in Release → `dotnet publish` di `RDS.ExpenseTracker.Functions` → upload artifact `functions-app` (con `include-hidden-files: true`, necessario per la cartella `.azurefunctions` generata dal worker SDK).
2. **migrate** — chiama il workflow riusabile `deploy-database.yml` (vedi sotto) con `secrets: inherit`: le migration EF Core sono applicate **prima** del deploy, così il nuovo codice trova già lo schema aggiornato.
3. **deploy** (`needs: [build, migrate]`) — environment GitHub `production`, login OIDC (`azure/login@v2`) con la stessa identità di `deploy-infra.yml` → legge `functionAppName` dagli output dell'ultimo deploy infra riuscito (`az deployment group list -g $AZURE_RESOURCE_GROUP`, deployment `infra-*`) → download artifact → `Azure/functions-action@v1` (piano **Flex Consumption**: `sku: flexconsumption`, One Deploy, niente slot), autenticato con le credenziali Azure della sessione (RBAC: `Contributor` sul resource group, concesso dal bootstrap).

Nessuna configurazione propria: usa i secret `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_SUBSCRIPTION_ID` e la variabile `AZURE_RESOURCE_GROUP` dell'environment `production`, già impostati per la pipeline infra (vedi [13-infrastructure-as-code](13-infrastructure-as-code.tech.md)). Niente publish profile: le *SCM Basic Auth Publishing Credentials* restano disattivate (`enableScmBasicAuth = false`). La CORS di piattaforma e le app settings sono definite dal template Bicep.

## Workflow migration (`.github/workflows/deploy-database.yml`)
`on: workflow_call` (da `deploy-backend.yml`) + `workflow_dispatch` (esecuzione manuale, es. dopo un restore del database). Un solo job `migrate`, environment `production`, `concurrency: deploy-database` senza cancellazione (mai due run di migration in parallelo):

1. `dotnet tool install --global dotnet-ef --version 10.0.7` (allineato ai pacchetti `Microsoft.EntityFrameworkCore.*`) → `dotnet ef migrations bundle -p api/RDS.ExpenseTracker.Infrastructure -s api/RDS.ExpenseTracker.Functions --configuration Release -o ./efbundle`.
2. Login OIDC (`azure/login@v2`) → legge `sqlServerFqdn` / `sqlDatabaseName` dagli output dell'ultimo deploy `infra-*`; il resource group del server è ricavato con `az sql server list` (può essere `SQL_SERVER_RESOURCE_GROUP`).
3. Apre una regola firewall temporanea `github-actions-<run_id>-<attempt>` sull'IP del runner (i runner GitHub non rientrano in *Allow Azure services*).
4. `./efbundle --connection "...;Authentication=Active Directory Default;"`: il `DefaultAzureCredential` di SqlClient usa il login Azure CLI dell'identità di deploy. Il bundle applica solo le migration mancanti (`__EFMigrationsHistory`) e mantiene la retry policy di `AddInfrastructureServices` (copre il risveglio del database serverless in pausa).
5. Rimuove la regola firewall (`if: always()`).

Prerequisiti una tantum: utente DB dell'identità di deploy (`ExpenseTracker GitHub deploy`, `db_ddladmin` + `db_datareader` + `db_datawriter`) creato da `infra/scripts/grant-function-sql-access.sql`, e ruolo `SQL Server Contributor` sul resource group del SQL server (bootstrap; se il server è nel resource group dell'app basta il `Contributor` già concesso). Vedi [13-infrastructure-as-code](13-infrastructure-as-code.tech.md).

**Nota**: il workflow non esegue `dotnet test` — il deploy avviene senza gate di test automatico.

## Hosting
Le risorse Azure (Function App, storage, Key Vault, Static Web App, monitoraggio; SQL server e database esistenti sono solo referenziati) sono definite in Bicep in `infra/` e deployate da `deploy-infra.yml`: vedi [13-infrastructure-as-code](13-infrastructure-as-code.tech.md). Le app settings elencate sotto sono impostate dal template.

Function App Linux o Windows, runtime `dotnet-isolated`, Functions v4, .NET 10. Il progetto è `api/RDS.ExpenseTracker.Functions` (`OutputType=Exe`, `Microsoft.Azure.Functions.Worker.Sdk`).

## Configurazione
Ordine di caricamento nel worker (`Program.cs`): `appsettings.json` → `appsettings.{AZURE_FUNCTIONS_ENVIRONMENT}.json` → variabili d'ambiente (incluse le app settings della Function App e i `Values` di `local.settings.json`).

- **`appsettings.json`** (copiato in output e pubblicato) — sezioni strutturate: `Logging`, `AzureAd` (vuota), `TradeRepublicCsv`, `BbvaCsv`, `SatisPayCsv`, `SellaCsv` (nomi dei conti di default; gli IBAN per riconoscere i trasferimenti non sono configurazione ma dati, su `Accounts.Iban`), `TransferMatching:Rules[]`, `App`.
- **`appsettings.Development.json`** — override locali (es. `ConnectionStrings:DefaultConnection`); gitignored e mai pubblicato.
- **`host.json`** — configurazione dell'host Functions: `routePrefix: "api"`, sampling Application Insights e `extensions.queues` per la coda `onedrive-sync` (`batchSize: 1`, `newBatchThreshold: 0`, `visibilityTimeout: 00:00:30`, `maxDequeueCount: 5`).
- **`local.settings.json`** — solo locale, gitignored. Template committato: `local.settings.example.json` (`FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`, `AZURE_FUNCTIONS_ENVIRONMENT=Development`, `AzureAd__*`, `DevAuth__Enabled=true` (bypass di autenticazione per il FE locale, vedi [04-auth-and-request-pipeline](../../01-platform/tech/04-auth-and-request-pipeline.tech.md)), `DemoAuth__Enabled=false` / `DemoAuth__SigningKey` / `DemoAuth__TokenLifetimeMinutes` (login demo, spento di default), `ConnectionStrings:DefaultConnection`, `Host.LocalHttpPort=7120`, `Host.CORS`, gli schedule dei timer `DemoResetSchedule` / `OneDriveSubscriptionRenewalSchedule` (obbligatori per l'indicizzazione) con i rispettivi flag `AzureWebJobs.<function>.Disabled=true`, la sezione `OneDriveImport__*` con `Enabled=false`). La coda `onedrive-sync` usa `AzureWebJobsStorage=UseDevelopmentStorage=true`: serve Azurite in esecuzione.

### App settings richieste su Azure
- `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`, `AzureWebJobsStorage` (create con la Function App).
- `AzureAd__TenantId`, `AzureAd__ClientId`, `AzureAd__Audience` (opzionale).
- Connection string `DefaultConnection` (tipo SQLAzure → esposta come `ConnectionStrings:DefaultConnection`).
- `APPLICATIONINSIGHTS_CONNECTION_STRING`.
- **Non** impostare `DevAuth__Enabled`: fuori da `Development` l'host non si avvia.
- Per il login demo (opzionale): `DemoAuth__Enabled=true`, `DemoAuth__SigningKey` (stringa casuale di almeno 32 byte, es. `openssl rand -base64 48`; meglio come riferimento Key Vault) ed eventualmente `DemoAuth__TokenLifetimeMinutes`. Con chiave mancante o troppo corta l'host non si avvia. Ruotare la chiave invalida tutti i token demo emessi. Serve anche che l'utente di test esista, quindi che il job di `Import_TestSeed` sia già girato almeno una volta.
- Per l'import automatico da OneDrive (opzionale): `OneDriveImport__Enabled=true`, `OneDriveImport__ClientId`, `OneDriveImport__FolderId`, `OneDriveImport__TargetUserEmail`, `OneDriveImport__KeyVaultUri`, `OneDriveImport__ClientState` (riferimento Key Vault), `OneDriveSubscriptionRenewalSchedule`; facoltativi `OneDriveImport__NotificationUrl` (default da `WEBSITE_HOSTNAME`), `OneDriveImport__SubscriptionResource`, `OneDriveImport__RefreshTokenSecretName` (default `OneDriveRefreshToken`), `OneDriveImport__ImportedFolderName` / `FailedFolderName` (default `Importati` / `Errori`). Tutti valorizzati da Bicep, vedi [13-infrastructure-as-code](13-infrastructure-as-code.tech.md).

## Esecuzione locale
```powershell
copy api\RDS.ExpenseTracker.Functions\local.settings.example.json api\RDS.ExpenseTracker.Functions\local.settings.json
cd api\RDS.ExpenseTracker.Functions
func start   # Azure Functions Core Tools v4 → http://localhost:7120/api/...
```
Il task VS Code `Avvia FE + BE` avvia `func start` insieme al dev server Vite; la configurazione `Attach to .NET Functions` in `.vscode/launch.json` permette il debug del processo worker. Gli script in `scripts/` puntano a `http://localhost:7120/api/import/...`.
