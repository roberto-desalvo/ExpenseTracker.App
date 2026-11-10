# 5. Operazioni

L'ambiente Azure è descritto interamente in Bicep (`infra/`) e distribuito da GitHub Actions, che si autentica con OIDC senza secret di lunga durata. Restano manuali solo pochi passi una tantum che richiedono privilegi o consenso interattivo.

## Infrastruttura (`infra/`)

| File | Risorse |
|---|---|
| `main.bicep` | Entry point (scope resource group): naming, tag, moduli, output; referenzia SQL server e database già esistenti |
| `modules/entra.bicep` | App registration API (scope `access_as_user` ed `execute_script`, ruolo `Files.Sender`), SPA, OneDrive import |
| `modules/functionApp.bicep` | Piano Flex Consumption + Function App, managed identity, CORS, app settings, ruoli RBAC |
| `modules/storage.bicep` | Storage dell'host Functions, senza shared key: code `onedrive-sync` e `bank-file-import`, container `bank-imports` per i file caricati dall'app (cancellati dopo 30 giorni da una lifecycle policy) |
| `modules/keyVault.bicep` | Key Vault con RBAC |
| `modules/monitoring.bicep` | Log Analytics + Application Insights |
| `modules/staticWebApp.bicep` | Static Web App del frontend, dominio custom opzionale |
| `parameters/prod.bicepparam` | Parametri di produzione, letti dalle variabili d'ambiente (`infra/.env` in locale) |
| `bootstrap/` | Deploy una tantum (scope subscription): resource group, identità di deploy GitHub, permessi Graph, RBAC, assegnazione `Files.Sender` |

### Passi una tantum

| Passo | Come |
|---|---|
| Bootstrap (resource group, identità OIDC, secret e variabili GitHub) | `./infra/scripts/bootstrap.ps1` |
| Utenti SQL della managed identity e dell'identità di deploy | `infra/scripts/grant-function-sql-access.sql` |
| Dominio custom | Record DNS TXT e CNAME (o ALIAS per un apex) verso la Static Web App |

## CI/CD (`.github/workflows/`)

Tutti i workflow usano l'environment GitHub `production` e leggono nomi e URL delle risorse dagli output dell'ultimo deploy infra.

| Workflow | Trigger | Cosa fa |
|---|---|---|
| `deploy-infra.yml` | Push/PR su `infra/**` | Validate e lint → what-if (anche sulle PR) → deploy (solo su `main`) |
| `deploy-database.yml` | Chiamato dal backend, o manuale | Migration bundle EF Core applicato al database, con regola firewall temporanea per il runner |
| `deploy-backend.yml` | Push su `api/**` | Build e publish → migration → deploy della Function App |
| `deploy-frontend.yml` | Push/PR su `web/**` | Build con le variabili `VITE_*` ricavate dall'infra → deploy sulla Static Web App (ambienti di preview per le PR) |

I workflow non eseguono i test: il deploy non ha un gate automatico.

## Configurazione

**Backend** — `appsettings.json` (sotto-conto trading di Trade Republic, regole `TransferMatching` per tipo di conto), poi `appsettings.{ambiente}.json`, poi variabili d'ambiente / app settings. In Azure le app settings sono scritte da Bicep. In locale si copia `local.settings.example.json` in `local.settings.json`; code e blob degli import richiedono Azurite.

| Sezione | Uso |
|---|---|
| `AzureAd` | Tenant, client ID e audience dell'API |
| `ConnectionStrings:DefaultConnection` | Database (in Azure passwordless con managed identity) |
| `DevAuth` | Login di sviluppo, solo in `Development` |
| `DemoAuth` | Login demo: `Enabled`, `SigningKey` (≥ 32 byte, in Key Vault) |
| `OneDriveImport` | Import da OneDrive: `Enabled` (in Azure la variabile `ONEDRIVE_IMPORT_ENABLED`), `ClientId` dell'app registration, `FolderName` (default `Transazioni`), `KeyVaultUri`, `NotificationUrl` (default: host pubblico della Function App) |
| `DemoResetSchedule`, `OneDriveSubscriptionRenewalSchedule` | Orari NCRONTAB (UTC) dei timer |
| `TradeRepublicCsv`, `TransferMatching` | Opzioni degli importer: nome del sotto-conto per le righe TRADING, regole per collegare i trasferimenti (`AccountType1/2` = nomi dei tipi di conto) |
| `ImportStorage:ContainerName` | Container dei file caricati dall'app (default `bank-imports`), nello storage di `AzureWebJobsStorage` |

**Frontend** — `web/.env.local` (modello `web/.env.example`); `scripts/write-env.mjs` le scrive in `src/environments/env.generated.ts` prima di start, build, test e lint. L'ambiente di processo ha la precedenza (così la CI usa i propri valori).

| Variabile | Uso |
|---|---|
| `VITE_EXPENSE_TRACKER_API_BASE_URL` | URL assoluto dell'API |
| `VITE_MSAL_CLIENT_ID`, `VITE_MSAL_TENANT_ID`, `VITE_MSAL_API_SCOPE` | Login Entra ID |
| `VITE_DEV_AUTH_BYPASS` | `false` per usare MSAL anche con `npm start` |
| `VITE_DEMO_LOGIN_ENABLED` | `false` per nascondere "Prova la demo" |

## Esecuzione locale

```powershell
# API su http://localhost:7120/api
cd api/RDS.ExpenseTracker.Functions; func start

# Frontend su http://localhost:5173
cd web; npm install; npm start
```

## Test

| Ambito | Strumenti | Cosa copre |
|---|---|---|
| Backend (`RDS.ExpenseTracker.Tests`) | xUnit, FluentAssertions; fake scritte a mano al posto dei mock | Parsing degli importer (anche su file reali), deduplica, matching e riconciliazione dei trasferimenti, categorizzazione, log degli import, dati demo e di test |
| Frontend (`*.spec.ts` accanto al codice) | Vitest via `ng test` | Store della tabella movimenti, degli import e del collegamento OneDrive, interceptor HTTP, `ThemeService` |

ArchUnitNET è referenziato ma non ci sono ancora regole di architettura.

```powershell
dotnet test ./api/RDS.ExpenseTracker.Tests/RDS.ExpenseTracker.Tests.csproj
cd web; npm test
```
