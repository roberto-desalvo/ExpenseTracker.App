# Infrastruttura Azure (Bicep)

Infrastructure as Code dell'ambiente ExpenseTracker: risorse Azure **e** app registration Entra ID
(estensione Microsoft Graph per Bicep). Documentazione:
[`docs/05-operazioni.md`](../docs/05-operazioni.md).

Input esterni: solo il nome del **resource group** (creato dal bootstrap se non esiste), il **SQL server** e il
**database** già esistenti (più il repository GitHub per le credenziali OIDC). Tenant e subscription si ricavano dal login.

```
infra/
├── bicepconfig.json               # estensione microsoftGraphV1 (app registration)
├── main.bicep                     # ambiente applicativo (scope: resource group) — deployato dalla pipeline
├── modules/
│   ├── entra.bicep                # app registration API (scope, ruolo Files.Sender) + SPA + OneDrive import
│   ├── monitoring.bicep           # Log Analytics + Application Insights
│   ├── storage.bicep              # storage host Functions + container del pacchetto
│   ├── functionApp.bicep          # piano Flex Consumption + Function App + ruoli RBAC
│   ├── keyVault.bicep             # Key Vault (RBAC) + segreto DemoAuth
│   └── staticWebApp.bicep         # Static Web App del frontend (+ dominio custom opzionale)
├── parameters/
│   └── prod.bicepparam            # parametri di produzione (valori da env var)
├── bootstrap/                     # una tantum, in locale, da admin (scope: subscription)
│   ├── main.bicep                 # identità di deploy GitHub (OIDC), permessi Graph, RBAC, ruoli Files.Sender
│   ├── bootstrap.bicepparam
│   └── modules/
├── scripts/
│   ├── bootstrap.ps1              # .env + bootstrap Bicep + environment/secret/variabili GitHub
│   ├── connect-onedrive.ps1       # consenso OneDrive (refresh token in Key Vault) + subscription Graph
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
| Cosa crea | Resource group dell'app (se manca), identità di deploy GitHub, permesso Graph `Application.ReadWrite.OwnedBy`, RBAC, assegnazioni `Files.Sender` (utenti) | App registration API/SPA/OneDrive, Function App, storage, Key Vault, Static Web App, monitoraggio |

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
   `SQL_SERVER_RESOURCE_GROUP`, `DEMO_AUTH_ENABLED`, `CUSTOM_DOMAIN`, `ONEDRIVE_IMPORT_ENABLED`,
   `ONEDRIVE_TARGET_USER_EMAIL` (vedi sotto).
4. In alternativa ai passi 1-3 a mano: `scripts/bootstrap.ps1` (vedi sotto).
5. Eseguire la pipeline `Deploy Infrastructure (Bicep)` (o deploy locale, vedi sotto).
6. Impostare `FILES_SENDER_ASSIGN=true` e `FILES_SENDER_UPNS` in `infra/.env` e rieseguire il bootstrap
   (passo 1): assegna il ruolo `Files.Sender` della nuova API agli utenti.
7. **Import da OneDrive**: con `ONEDRIVE_IMPORT_ENABLED=true` e `ONEDRIVE_TARGET_USER_EMAIL` impostati (e il
   deploy eseguito), lanciare `./infra/scripts/connect-onedrive.ps1` e accedere con l'account Microsoft del
   OneDrive (vedi [Import da OneDrive](#import-da-onedrive)). Il consenso non è automatizzabile.
8. Eseguire [`scripts/grant-function-sql-access.sql`](scripts/grant-function-sql-access.sql) sul database
   come amministratore Entra ID (utenti DB della managed identity e dell'identità di deploy GitHub, che applica
   le migration: T-SQL, non gestibile da Bicep).
9. Le pipeline applicative (`deploy-backend.yml`, `deploy-frontend.yml`) non hanno configurazione propria:
   usano lo stesso environment `production` (login OIDC + `AZURE_RESOURCE_GROUP`) e leggono dagli output
   dell'ultimo deploy infra riuscito il nome della Function App, i valori `VITE_*` e la Static Web App (il cui
   deployment token è letto a runtime con `az staticwebapp secrets list`). Niente publish profile né token da copiare.
   `deploy-backend.yml` chiama prima [`deploy-database.yml`](../.github/workflows/deploy-database.yml), che applica le
   migration EF Core (migration bundle) aprendo una regola firewall temporanea per l'IP del runner (ruolo
   `SQL Server Contributor` concesso dal bootstrap); si può lanciare anche a mano (`workflow_dispatch`).
   Per gli script di import in `tools/scripts`: passare `-Audience <output apiIdentifierUri>`.

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

## Import da OneDrive

L'import dei file salvati nella cartella OneDrive `oneDriveFolderId` (`/Transazioni`, sottocartelle BBVA /
Sella / Satispay / Trade_Republic / Excel) non è più una Logic App: lo fanno le function `OneDrive_*` della
Function App. Microsoft Graph notifica ogni modifica della cartella, la function importa i nuovi file e li
sposta in `<Banca>/Importati` o `<Banca>/Errori`; il timer `OneDrive_SubscriptionRenewal` (parametro
`oneDriveSubscriptionRenewalSchedule`, default `0 0 4 * * *` UTC) rinnova la subscription Graph.

Il OneDrive è personale (account Microsoft), quindi la Function App usa un token delegato:

```powershell
. ./infra/scripts/load-env.ps1          # AZURE_RESOURCE_GROUP
./infra/scripts/connect-onedrive.ps1    # device code: accedere con l'account del OneDrive
```

Lo script legge gli output dell'ultimo deploy e salva in Key Vault il refresh token (`OneDriveRefreshToken`) e
il `clientState` delle notifiche (`OneDriveClientState`). Poi riavvia la Function App e crea la subscription.
Richiede `Key Vault Secrets Officer` sul vault per l'utente che lo esegue. Va rieseguito solo se il token viene
revocato o resta inutilizzato per 90 giorni. I file già presenti nella cartella al momento dell'attivazione non
vengono importati.

**Rimozione della vecchia Logic App**: il deploy incrementale non elimina le risorse tolte dal template.
Dopo il primo deploy di questa versione:

```powershell
az logic workflow delete -g <resource-group> -n logic-<appName>-import-<env> --yes
az resource delete -g <resource-group> -n onedrive --resource-type Microsoft.Web/connections
az identity delete -g <resource-group> -n id-<appName>-logic-<env>
```

Il bootstrap va rieseguito **prima** del deploy: autorizza la pipeline ad assegnare i nuovi ruoli della
Function App (`Storage Queue Data Contributor`, `Key Vault Secrets Officer`).

## Reset demo

Il **reset giornaliero dei dati demo** non è più una Logic App: è la timer function `Scheduled_DemoDataReset`
della Function App (seed dell'utente demo e poi riconciliazione dei trasferimenti, in sequenza). L'orario è
il parametro `demoResetSchedule` (NCRONTAB in **UTC**, default `0 0 3 * * *`), scritto nell'app setting
`DemoResetSchedule`. La vecchia `logic-<appName>-demo-reset-<env>` non viene cancellata dal deploy
incrementale: dopo il primo deploy eliminarla a mano con
`az logic workflow delete -g <resource-group> -n logic-<appName>-demo-reset-<env> --yes`.

## Dominio custom

`CUSTOM_DOMAIN` (es. `www.expensetracker.lol`, apex o sottodominio) aggiunge il dominio alla Static Web App, ai
redirect URI della SPA, alla CORS dell'API e lo usa come `App__FEUrl`. Un dominio può stare su un solo sito: va
impostato **dopo** averlo rimosso dalla vecchia Static Web App. DNS: CNAME (sottodominio) o ALIAS (apex) verso
`staticWebAppDefaultHostname` + record TXT `_dnsauth[.<sottodominio>]` con il token di validazione (output
`customDomainValidationToken`, vuoto quando il dominio è già validato; entrambi nel job summary).
