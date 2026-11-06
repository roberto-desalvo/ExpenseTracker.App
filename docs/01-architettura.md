# 1. Architettura

Il sistema è una SPA Angular che parla con un'API su Azure Functions, a sua volta appoggiata a un database SQL Server. L'autenticazione è delegata a Entra ID; l'import automatico ascolta le modifiche di una cartella OneDrive tramite Microsoft Graph. Tutto gira su servizi Azure a consumo, definiti in Bicep.

## Componenti del sistema

```mermaid
flowchart LR
    user([Utente])
    subgraph Azure
        swa[Static Web App<br/>SPA Angular]
        func[Function App<br/>.NET 10 isolated]
        queue[(Code<br/>onedrive-sync, bank-file-import)]
        blob[(Blob<br/>bank-imports)]
        sql[(Azure SQL<br/>serverless)]
        kv[Key Vault]
        ai[Application Insights]
    end
    entra[Entra ID]
    graph[Microsoft Graph<br/>OneDrive personale]

    user --> swa
    swa -- login MSAL --> entra
    swa -- REST + bearer token --> func
    func -- EF Core --> sql
    func -- valida JWT --> entra
    graph -- change notification --> func
    func -- accoda --> queue
    func -- file caricati dall'app --> blob
    queue -- queue trigger --> func
    func -- delta query, download, spostamento file --> graph
    func -- refresh token, segreti --> kv
    func -.-> ai
```

| Componente | Responsabilità |
|---|---|
| **SPA Angular** (Static Web App) | Interfaccia utente, login MSAL, chiamate REST all'API |
| **Function App** | API REST, regole di business, import dei file, function pianificate |
| **Azure SQL** | Dati dell'applicazione (database serverless già esistente, solo referenziato da Bicep) |
| **Coda `onedrive-sync`** | Disaccoppia la notifica di Graph (risposta immediata) dall'import vero e proprio |
| **Coda `bank-file-import` + container `bank-imports`** | Import asincrono dei file caricati dall'app: l'upload salva il file e risponde subito, l'elaborazione avviene in coda |
| **Key Vault** | Refresh token OneDrive, `clientState` delle notifiche, chiave di firma dei token demo |
| **Entra ID** | App registration API (scope `access_as_user`, ruolo `Files.Sender`), SPA e app OneDrive |
| **Microsoft Graph** | Accesso al OneDrive personale con token delegato |
| **Application Insights** | Log e telemetria dell'API |

### Function pianificate e in background

| Function | Trigger | Cosa fa |
|---|---|---|
| `OneDrive_Notifications` | HTTP (anonimo, verificato con `clientState`) | Riceve le notifiche Graph e accoda un messaggio |
| `OneDrive_Sync` | Coda `onedrive-sync` | Importa i file nuovi della cartella OneDrive |
| `OneDrive_SubscriptionRenewal` | Timer giornaliero | Crea o rinnova la subscription Graph |
| `ImportJob_Process` | Coda `bank-file-import` | Importa un file caricato dall'app e aggiorna lo stato in `ImportLogs` |
| `Scheduled_DemoDataReset` | Timer giornaliero | Rigenera i dati dell'utente demo (preferenze di aspetto e layout di Analisi compresi) e riconcilia i trasferimenti |

## Scelte tecniche principali

| Scelta | Motivo |
|---|---|
| **Azure Functions, isolated worker .NET 10, piano Flex Consumption** | Costo a consumo per un'app personale; HTTP, code e timer nello stesso host |
| **Clean Architecture** (Domain, Application, Infrastructure, Functions) | Regole di business indipendenti da database e hosting; dipendenze a senso unico |
| **EF Core code-first su SQL Server** | Schema versionato con migration, applicate dalla CI prima di ogni deploy |
| **FluentResults** nei servizi | Gli errori di business sono valori, non eccezioni; la function li traduce in status HTTP (problem details) |
| **Validazione JWT nel middleware del worker** | Nessuna dipendenza da Easy Auth; stesso controllo su tutte le function, con eccezioni esplicite via attributo |
| **Dati sempre filtrati per utente** | Ogni utente vede solo conti, movimenti e categorie propri; l'utente interno nasce al primo accesso (JIT) |
| **Deduplica tramite `ExternalId`** | Reimportare lo stesso estratto conto non crea doppioni |
| **Conto degli import dal tipo di conto** | Il conto di destinazione non dipende dal nome scelto dall'utente: un tipo (banca) per conto, nessun conto creato in automatico |
| **Import dall'app asincrono (blob + coda)** | L'upload risponde subito anche con file lenti da elaborare; lo stato in `ImportLogs` alimenta lo storico e l'avanzamento in UI |
| **Import OneDrive con notifiche Graph + coda** | Import quasi in tempo reale; il token delegato è necessario perché il OneDrive è personale (Graph non ammette accesso app-only) |
| **Angular 22 standalone, signals, zoneless** | Stato reattivo semplice senza NgRx: store `providedIn: root` basati su signal |
| **`@azure/msal-browser` diretto** (senza `msal-angular`) | Le tre modalità di accesso (Entra ID, demo, sviluppo) gestite in un solo `AuthService` |
| **Angular Material + token CSS** | Componenti pronti, stile personalizzabile con preset scritti come variabili CSS |
| **ECharts** (`ngx-echarts`, caricato in lazy) | Grafici ricchi senza appesantire il primo caricamento |
| **Bicep + estensione Microsoft Graph** | Risorse Azure e app registration Entra ID nello stesso template |
| **GitHub Actions con OIDC** | Nessun secret di lunga durata: la pipeline si autentica con credenziali federate |
| **Accesso passwordless** | Function App verso SQL, storage, Key Vault e Application Insights con managed identity |
