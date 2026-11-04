# Infrastruttura Azure (Bicep)

Infrastructure as Code dell'ambiente ExpenseTracker: risorse Azure **e** app registration Entra ID
(estensione Microsoft Graph per Bicep). Documentazione completa:
[`docs/be/04-operations/tech/13-infrastructure-as-code.tech.md`](../docs/be/04-operations/tech/13-infrastructure-as-code.tech.md).

Input esterni: solo il nome del **resource group** (creato dal bootstrap se non esiste), il **SQL server** e il
**database** già esistenti (più il repository GitHub per le credenziali OIDC). Tenant e subscription si ricavano dal login.

```
infra/
├── bicepconfig.json               # estensione microsoftGraphV1 (app registration)
├── main.bicep                     # ambiente applicativo (scope: resource group) — deployato dalla pipeline
├── modules/
│   ├── entra.bicep                # app registration API (scope, ruolo Files.Sender) + SPA
│   ├── monitoring.bicep           # Log Analytics + Application Insights
│   ├── storage.bicep              # storage host Functions + container del pacchetto
│   ├── functionApp.bicep          # piano Flex Consumption + Function App + ruoli RBAC
│   ├── keyVault.bicep             # Key Vault (RBAC) + segreto DemoAuth
│   ├── logicApps.bicep            # Logic App import OneDrive + reset demo, identità, connessione OneDrive
│   └── staticWebApp.bicep         # Static Web App del frontend (+ dominio custom opzionale)
├── logicapps/                     # definizioni dei workflow (JSON del designer con segnaposto __X__)
├── parameters/
│   └── prod.bicepparam            # parametri di produzione (valori da env var)
├── bootstrap/                     # una tantum, in locale, da admin (scope: subscription)
│   ├── main.bicep                 # identità di deploy GitHub (OIDC), permessi Graph, RBAC, ruoli Files.Sender
│   ├── bootstrap.bicepparam
│   └── modules/
├── scripts/
│   ├── bootstrap.ps1              # .env + bootstrap Bicep + environment/secret/variabili GitHub
│   ├── grant-function-sql-access.sql
│   ├── load-env.ps1               # carica infra/.env nella sessione PowerShell
│   └── load-env.sh                # idem per bash (da usare con source)
├── .env.example                   # modello delle variabili d'ambiente
└── .env                           # valori locali, gitignored (NON committare)
```

## Due livelli

| | `bootstrap/` | `main.bicep` |
|---|---|---|
| Chi lo esegue | Tu, in locale, una tantum (e quando cambiano i permessi) | La pipeline `deploy-infra.yml` |
| Privilegi richiesti | Owner sulla subscription + *Privileged Role Administrator* (o Global Admin) in Entra ID | Quelli concessi dal bootstrap |
| Cosa crea | Resource group dell'app (se manca), identità di deploy GitHub, permesso Graph `Application.ReadWrite.OwnedBy`, RBAC, assegnazioni `Files.Sender` (utenti + identità Logic App) | App registration API/SPA, Function App, storage, Key Vault, Static Web App, Logic App, monitoraggio |

La separazione serve perché la pipeline non può creare la propria identità né concedersi permessi:
le operazioni privilegiate (consenso admin di permessi Graph, assegnazione di app role agli utenti)
restano fuori dalla CI.

## Primo avvio

```powershell
copy infra\.env.example infra\.env      # poi compilarlo
az login
. .\infra\scripts\load-env.ps1 -SetSubscription

# 1. Bootstrap: identità di deploy + permessi
az deployment sub create -l $env:AZURE_LOCATION -n expensetracker-bootstrap `
  --parameters infra/bootstrap/bootstrap.bicepparam --query properties.outputs
```

2. Creare l'**environment** GitHub `production` (opzionale: required reviewers, che bloccano anche il what-if
   delle PR): variabili e secret stanno lì e tutti i job della pipeline lo referenziano.
3. **Secrets** dell'environment: gli output `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` e
   (opzionale) `DEMO_AUTH_SIGNING_KEY`. **Variables** dell'environment: `AZURE_RESOURCE_GROUP`, `SQL_SERVER_NAME`, `SQL_DATABASE_NAME`,
   `SQL_SERVER_RESOURCE_GROUP`, `DEMO_AUTH_ENABLED`, `CUSTOM_DOMAIN` (vedi sotto).
4. In alternativa ai passi 1-3 a mano: `scripts/bootstrap.ps1` (vedi sotto).
5. Eseguire la pipeline `Deploy Infrastructure (Bicep)` (o deploy locale, vedi sotto).
6. Impostare `FILES_SENDER_ASSIGN=true` e `FILES_SENDER_UPNS` in `infra/.env` e rieseguire il bootstrap
   (passo 1): assegna il ruolo `Files.Sender` della nuova API agli utenti e all'identità delle Logic App.
7. **Autorizzare la connessione OneDrive** nel portale (connessione API `onedrive` → *Modifica connessione API*
   → *Autorizza* → *Salva*): il consenso OAuth non è automatizzabile. Finché non è autorizzata la Logic App
   di import fallisce.
8. Eseguire [`scripts/grant-function-sql-access.sql`](scripts/grant-function-sql-access.sql) sul database
   come amministratore Entra ID (utente DB della managed identity: T-SQL, non gestibile da Bicep).
9. Allineare le pipeline applicative con gli output del deploy (riepilogati nel job summary):
   - `deploy-frontend.yml`: secrets `VITE_MSAL_CLIENT_ID`, `VITE_MSAL_TENANT_ID`, `VITE_MSAL_API_SCOPE`,
     `VITE_EXPENSE_TRACKER_API_BASE_URL` e il deployment token della Static Web App
     (`az staticwebapp secrets list -n <swa> --query properties.apiKey`);
   - `deploy-backend.yml`: variabile `AZURE_FUNCTIONAPP_NAME` e publish profile
     (`az functionapp deployment list-publishing-profiles`).
   - script di import in `tools/scripts`: passare `-Audience <output apiIdentifierUri>`.

### Script di bootstrap

[`scripts/bootstrap.ps1`](scripts/bootstrap.ps1) esegue in un colpo solo i passi 1-3: carica `infra/.env`, lancia
il bootstrap Bicep e, con la GitHub CLI (`winget install --id GitHub.cli`, poi `gh auth login`), crea l'environment
`production` e ne imposta secret (output del bootstrap + `DEMO_AUTH_SIGNING_KEY`) e variabili (da `.env`;
quelle vuote sono saltate). È idempotente e supporta `-WhatIf`.

```powershell
.\infra\scripts\bootstrap.ps1                # primo avvio
.\infra\scripts\bootstrap.ps1 -SkipGitHub    # passo 6 (Files.Sender), GitHub già configurato
.\infra\scripts\bootstrap.ps1 -SkipBootstrap # solo GitHub (es. dopo aver impostato CUSTOM_DOMAIN)
```

## Deploy locale

```powershell
. .\infra\scripts\load-env.ps1 -SetSubscription
az deployment group what-if -g $env:AZURE_RESOURCE_GROUP --parameters infra/parameters/prod.bicepparam
az deployment group create  -g $env:AZURE_RESOURCE_GROUP --parameters infra/parameters/prod.bicepparam
```

```bash
source infra/scripts/load-env.sh
az deployment group what-if -g "$AZURE_RESOURCE_GROUP" --parameters infra/parameters/prod.bicepparam
```

Il what-if non valuta le risorse Microsoft Graph (app registration): mostra solo le risorse Azure.

## SQL server e database

Sono risorse **già esistenti**: il template le legge soltanto (per ricavare l'FQDN e costruire la
connection string) e non le crea né le modifica. Il deploy fallisce se non esistono. Restano gestiti
a mano: amministratore Entra ID, firewall (serve la regola *Allow Azure services* perché la Function
App Flex Consumption non ha IP in uscita fissi), SKU e backup.

## App registration

`modules/entra.bicep` crea due app registration single-tenant, identificate da `uniqueName`
(`<appName>-api-<env>`, `<appName>-spa-<env>`), quindi idempotenti:

- **API**: identifier URI `api://<tenant-id>/<appName>-api-<env>`, token v2, scope `access_as_user`
  (frontend) ed `execute_script` (script di import, pre-autorizzato per Azure CLI), app role `Files.Sender`.
- **SPA**: redirect URI della Static Web App + `extraSpaRedirectUris` (default `http://localhost:5173`),
  pre-autorizzata sull'API (nessun consenso per `access_as_user`).

Gli utenti sono identificati dal claim `oid`, che non dipende dall'app registration: passando alle
nuove app i dati esistenti restano associati agli stessi utenti. Le vecchie app registration create a
mano non vengono toccate e si possono eliminare dopo il passaggio.

## Logic App

`modules/logicApps.bicep` ricrea le due Logic App con un'identità gestita user-assigned
(`id-<appName>-logic-<env>`) che chiama l'API con token per l'audience `apiIdentifierUri`:

- **import** (`logic-<appName>-import-<env>`): nuovi file nella cartella OneDrive `oneDriveFolderId`
  (`/Transazioni`, sottocartelle BBVA / Sella / Satispay / Trade_Republic / Excel) → `/api/import/<banca>-csv/stream` o `-xlsx`;
- **demo reset** (`logic-<appName>-demo-reset-<env>`): ogni giorno `/api/import/test-seed` e `/api/import/transfers/reconcile`.

Le definizioni sono in `logicapps/*.json` (esportate dal designer): URL del backend, audience, identità e
cartella sono segnaposto `__X__` sostituiti dal template. Per modificare un workflow: modificarlo nel
designer, esportare `properties.definition`, rimettere i segnaposto e aggiornare il file. Lo storico delle
esecuzioni va in Log Analytics (diagnostic settings + soluzione *Logic Apps Management*).

## Dominio custom

`CUSTOM_DOMAIN` (es. `expensetracker.lol`, dominio apex) aggiunge il dominio alla Static Web App, ai redirect
URI della SPA, alla CORS dell'API e lo usa come `App__FEUrl`. Un dominio può stare su un solo sito: va
impostato **dopo** averlo rimosso dalla vecchia Static Web App. Validazione con record TXT (token
nell'output `customDomainValidationToken` e nel job summary) + record ALIAS/A verso `staticWebAppDefaultHostname`.
