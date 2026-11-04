# Infrastruttura Azure (Bicep)

Infrastructure as Code dell'ambiente ExpenseTracker, deployata dalla pipeline
[`.github/workflows/deploy-infra.yml`](../.github/workflows/deploy-infra.yml).
Documentazione completa: [`docs/be/04-operations/tech/13-infrastructure-as-code.tech.md`](../docs/be/04-operations/tech/13-infrastructure-as-code.tech.md).

```
infra/
├── main.bicep                     # entry point (scope: resource group)
├── modules/
│   ├── monitoring.bicep           # Log Analytics + Application Insights
│   ├── storage.bicep              # storage host Functions + container del pacchetto
│   ├── functionApp.bicep          # piano Flex Consumption + Function App + ruoli RBAC
│   ├── sql.bicep                  # riferimento (existing) a SQL server + database già esistenti
│   ├── keyVault.bicep             # Key Vault (RBAC) + segreto DemoAuth
│   └── staticWebApp.bicep         # Static Web App del frontend
├── parameters/
│   └── prod.bicepparam            # parametri di produzione (valori sensibili da env var)
├── scripts/
│   ├── grant-function-sql-access.sql
│   ├── load-env.ps1               # carica infra/.env nella sessione PowerShell
│   └── load-env.sh                # idem per bash (da usare con source)
├── .env.example                   # modello delle variabili d'ambiente del deploy
└── .env                           # valori locali, gitignored (NON committare)
```

## Deploy locale

Le variabili d'ambiente stanno in `infra/.env` (**gitignored**, può contenere segreti; modello
committato: `infra/.env.example`). Si caricano nella sessione con `scripts/load-env.ps1`
(o `scripts/load-env.sh`), che segnala le variabili obbligatorie ancora vuote.

```powershell
copy infra\.env.example infra\.env   # solo la prima volta, poi compilarlo
az login
. .\infra\scripts\load-env.ps1 -SetSubscription   # -Path per un altro file .env
az group create -n $env:AZURE_RESOURCE_GROUP -l $env:AZURE_LOCATION   # se non esiste
az deployment group what-if -g $env:AZURE_RESOURCE_GROUP --parameters infra/parameters/prod.bicepparam
az deployment group create  -g $env:AZURE_RESOURCE_GROUP --parameters infra/parameters/prod.bicepparam
```

```bash
source infra/scripts/load-env.sh
az deployment group what-if -g "$AZURE_RESOURCE_GROUP" --parameters infra/parameters/prod.bicepparam
```

## Setup della pipeline (una tantum)

1. **Identità di deploy**: App registration (o managed identity) con federated credentials GitHub per
   `repo:roberto-desalvo/ExpenseTracker.App:environment:production` (deploy) e
   `repo:roberto-desalvo/ExpenseTracker.App:pull_request` (what-if).
2. **Ruoli** sul resource group: `Contributor` + `Role Based Access Control Administrator`
   (il template crea role assignment per la managed identity della Function App).
   Se il resource group non esiste ancora serve `Contributor` sulla subscription per crearlo.
   Se il SQL server sta in un altro resource group, sull'altro resource group basta `Reader`.
3. **Secrets** GitHub: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`,
   opzionale `DEMO_AUTH_SIGNING_KEY`.
4. **Variables** GitHub: `AZURE_RESOURCE_GROUP`, `AZURE_LOCATION`, `AZURE_AD_TENANT_ID`,
   `AZURE_AD_CLIENT_ID`, `AZURE_AD_AUDIENCE`, `SQL_SERVER_NAME`, `SQL_DATABASE_NAME`,
   `SQL_SERVER_RESOURCE_GROUP`, `DEMO_AUTH_ENABLED`.

## SQL server e database

Sono risorse **già esistenti**: il template le legge soltanto (per ricavare l'FQDN e costruire la
connection string) e non le crea né le modifica. Il deploy fallisce se non esistono. Restano gestiti
a mano: amministratore Entra ID, firewall (serve la regola *Allow Azure services* perché la Function
App Flex Consumption non ha IP in uscita fissi), SKU e backup.
5. **Environment** GitHub `production` (opzionale: required reviewers per approvare il deploy).

## Dopo il primo deploy

1. Eseguire [`scripts/grant-function-sql-access.sql`](scripts/grant-function-sql-access.sql) sul database
   come amministratore Entra ID, sostituendo il nome della Function App.
2. Applicare le eventuali migrazioni EF mancanti (`dotnet ef database update ...`).
3. Allineare le pipeline applicative ai nuovi nomi:
   - `deploy-backend.yml`: variabile `AZURE_FUNCTIONAPP_NAME` e nuovo publish profile
     (`az functionapp deployment list-publishing-profiles`);
   - `deploy-frontend.yml`: nuovo deployment token della Static Web App
     (`az staticwebapp secrets list -n <swa> --query properties.apiKey`) e
     `VITE_EXPENSE_TRACKER_API_BASE_URL` = output `functionAppUrl`.

Per adottare la Function App esistente creata a mano, impostare `functionAppName` in `prod.bicepparam`
e verificare con attenzione l'output del what-if.
