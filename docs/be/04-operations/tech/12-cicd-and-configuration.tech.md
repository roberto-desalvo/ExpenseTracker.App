# 12 - CI/CD and configuration (Tecnico)

## Workflow (`api/.github/workflows/`)
Due workflow GitHub Actions, entrambi `on: push: branches: [master]` + `workflow_dispatch`, `runs-on: windows-latest`:

- **`master_expensetracker-api.yml`** — build (`actions/setup-dotnet@v4`, .NET 9.x) → `dotnet build`/`publish` Release → deploy su Azure Web App `ExpenseTracker-Api` (slot `Production`, via `azure/webapps-deploy@v3`).
- **`master_moneytracker-api.yml`** — stesso build, deploy su `MoneyTracker-Api` (nome legacy/parallelo), con in più `azure/login@v2` (OIDC) e `az webapp config appsettings set` per iniettare `AZUREAD__*`/`KEYVAULT__*` da GitHub Secrets prima del deploy.

**Nota**: nessuno dei due workflow esegue `dotnet test` — il deploy avviene senza gate di test automatico. I workflow installano .NET 9.x mentre tutti i `.csproj` puntano a `net10.0` — da verificare/allineare.

## Configurazione (`appsettings.json` / `appsettings.Development.json`)
Sezioni principali: `Logging`, `AllowedHosts`, `AzureAd` (Instance/TenantId/ClientId/Audience), `TradeRepublicCsv`, `BbvaCsv`, `SatisPayCsv`, `SellaCsv` (default account + IBAN map), `TransferMatching:Rules[]` (solo in `appsettings.json` base), `App` (FEUrl/AllowedLocalOrigins).

`appsettings.Development.json` aggiunge `ConnectionStrings:DefaultConnection` per lo sviluppo locale — file non da committare con credenziali reali.

## Variabili d'ambiente
Connection string e impostazioni Azure AD sono sovrascrivibili via variabili d'ambiente (`AddEnvironmentVariables()` in `Program.cs`), coerentemente con il deploy su Azure App Service (`az webapp config appsettings set`).
