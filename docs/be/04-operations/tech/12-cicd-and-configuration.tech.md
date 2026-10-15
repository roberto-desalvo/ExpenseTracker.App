# 12 - CI/CD and configuration (Tecnico)

## Workflow (`.github/workflows/deploy-backend.yml`)
`on: push` su `main` (path `api/**` o il workflow stesso) + `workflow_dispatch`, runner `ubuntu-latest`:

1. **build** — `actions/setup-dotnet@v4` (.NET 10.x) → `dotnet build` della solution in Release → `dotnet publish` di `RDS.ExpenseTracker.Functions` → upload artifact `functions-app` (con `include-hidden-files: true`, necessario per la cartella `.azurefunctions` generata dal worker SDK).
2. **deploy** — download artifact → `azure/login@v2` (OIDC, secret `AZUREAPPSERVICE_CLIENTID_*`/`TENANTID_*`/`SUBSCRIPTIONID_*`) → sync CORS opzionale → `Azure/functions-action@v1` sulla Function App.

Variabili di repository (GitHub → Settings → Variables):

| Variabile | Obbligatoria | Uso |
|---|---|---|
| `AZURE_FUNCTIONAPP_NAME` | Sì | Nome della Function App di destinazione |
| `AZURE_RESOURCE_GROUP` | No | Resource group, usato solo dallo step CORS |
| `FE_URL` | No | Origine del frontend aggiunta alla CORS della Function App (idempotente) |

L'identità federata usata da `azure/login` deve avere un ruolo (es. *Website Contributor*) sulla Function App.

**Nota**: il workflow non esegue `dotnet test` — il deploy avviene senza gate di test automatico.

## Hosting
Function App Linux o Windows, runtime `dotnet-isolated`, Functions v4, .NET 10. Il progetto è `api/RDS.ExpenseTracker.Functions` (`OutputType=Exe`, `Microsoft.Azure.Functions.Worker.Sdk`).

## Configurazione
Ordine di caricamento nel worker (`Program.cs`): `appsettings.json` → `appsettings.{AZURE_FUNCTIONS_ENVIRONMENT}.json` → variabili d'ambiente (incluse le app settings della Function App e i `Values` di `local.settings.json`).

- **`appsettings.json`** (copiato in output e pubblicato) — sezioni strutturate: `Logging`, `AzureAd` (vuota), `TradeRepublicCsv`, `BbvaCsv`, `SatisPayCsv`, `SellaCsv` (default account + IBAN map), `TransferMatching:Rules[]`, `App`.
- **`appsettings.Development.json`** — override locali (es. `ConnectionStrings:DefaultConnection`); gitignored e mai pubblicato.
- **`host.json`** — configurazione dell'host Functions: `routePrefix: "api"` e sampling Application Insights.
- **`local.settings.json`** — solo locale, gitignored. Template committato: `local.settings.example.json` (`FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`, `AZURE_FUNCTIONS_ENVIRONMENT=Development`, `AzureAd__*`, `DevAuth__Enabled=true` (bypass di autenticazione per il FE locale, vedi [04-auth-and-request-pipeline](../../01-platform/tech/04-auth-and-request-pipeline.tech.md)), `ConnectionStrings:DefaultConnection`, `Host.LocalHttpPort=7120`, `Host.CORS`).

### App settings richieste su Azure
- `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`, `AzureWebJobsStorage` (create con la Function App).
- `AzureAd__TenantId`, `AzureAd__ClientId`, `AzureAd__Audience` (opzionale).
- Connection string `DefaultConnection` (tipo SQLAzure → esposta come `ConnectionStrings:DefaultConnection`).
- `APPLICATIONINSIGHTS_CONNECTION_STRING`.
- **Non** impostare `DevAuth__Enabled`: fuori da `Development` l'host non si avvia.

## Esecuzione locale
```powershell
copy api\RDS.ExpenseTracker.Functions\local.settings.example.json api\RDS.ExpenseTracker.Functions\local.settings.json
cd api\RDS.ExpenseTracker.Functions
func start   # Azure Functions Core Tools v4 → http://localhost:7120/api/...
```
Il task VS Code `Avvia FE + BE` avvia `func start` insieme al dev server Vite; la configurazione `Attach to .NET Functions` in `.vscode/launch.json` permette il debug del processo worker. Gli script in `scripts/` puntano a `http://localhost:7120/api/import/...`.
