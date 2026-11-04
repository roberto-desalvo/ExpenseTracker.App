# 03 - API surface and routing (Tecnico)

## Base
Il backend è un'app **Azure Functions (isolated worker, .NET 10)** con integrazione ASP.NET Core (`ConfigureFunctionsWebApplication`). Ogni endpoint è una function con `HttpTrigger(AuthorizationLevel.Anonymous, ...)` dentro una classe `*Functions` in `Functions/Functions/`. Il prefisso di route `api` è impostato in `host.json` (`extensions.http.routePrefix`), quindi i path pubblici sono invariati rispetto alla precedente Web API (`/api/account/...`, `/api/transaction/...`, ecc.; il routing è case-insensitive).

Tutte le classi (tranne `ImportFunctions` e `AuthFunctions`) ereditano da `FunctionBase`, che scompone `FluentResults.Result` in `IActionResult` (vedi [05-error-handling](05-error-handling.tech.md)) e offre gli helper `ReadBodyAsync<T>` (body JSON con le web defaults di ASP.NET Core: camelCase, case-insensitive; body nullo/malformato → 400), `GetQueryBool`, `GetQueryDate` e `GetQueryString`.

`AuthorizationLevel.Anonymous` significa solo che non serve una function key: l'autenticazione JWT è applicata a **tutte** le function da `AuthenticationMiddleware` (vedi [04-auth-and-request-pipeline](04-auth-and-request-pipeline.tech.md)).

Il nome di ogni function segue la convenzione `<Risorsa>_<Azione>` (es. `Account_Query`), visibile nel portale Azure e in Application Insights. I parametri di route numerici usano il vincolo `{id:int}`.

## `AuthFunctions` — `api/auth`
- `GET /` (`Auth_GetProfile`) — id utente interno (`{ userId }`) risolto dal token.
- `POST /demo` (`Auth_DemoLogin`, `[AllowAnonymousFunction]`) — login demo: emette un bearer token firmato dall'API per l'utente di test `test@dev.local` (`{ accessToken, expiresOn, userName }`). 404 se `DemoAuth:Enabled=false` o se l'utente di test non esiste ancora (lo crea `Import_TestSeed`). Vedi [04-auth-and-request-pipeline](04-auth-and-request-pipeline.tech.md).

## `AccountFunctions` — `api/account`
- `POST /query` — ricerca paginata account.
- `GET /{id}` — account singolo.
- `GET /{id}/availability` — saldo calcolato.
- `POST /` — creazione bulk.
- `PUT /` — update.

`AccountDto` include `iban` (opzionale): normalizzato server-side (maiuscolo, senza spazi), formato non valido → 400, IBAN già usato da un altro conto dello stesso utente → 409.

## `CategoryFunctions` — `api/category`
Tutti gli endpoint risolvono l'utente con `ICurrentUserAccessor.GetUserIdAsync()` e operano solo sulle sue categorie (categoria di un altro utente → 404).
- `POST /query` — ricerca paginata categorie.
- `GET /{id}` — categoria singola.
- `GET /default` — categoria di default.
- `POST /` — creazione bulk (le nuove categorie appartengono all'utente corrente, mai di sistema: `Kind = null`).
- `PUT /` — update.
- `DELETE /{id}` — elimina (riassegna prima le transazioni alla categoria di default).
- `POST /sync` — body `CategorySyncRequest { startDate, endDate }` (obbligatori, estremi inclusi, conta solo il giorno; `startDate > endDate` → 400). Risolve l'utente con `ICurrentUserAccessor` e riassegna la categoria ai suoi movimenti del periodo tramite i tag delle categorie (stessa regola dell'import, vedi [07-application-services-and-mapping](../../02-data-persistence/tech/07-application-services-and-mapping.tech.md)). Risposta `CategorySyncResultDto { examinedTransactions, updatedTransactions, unmatchedTransactions }`.

## `TransactionFunctions` — `api/transaction`
Tutte le route risolvono l'utente con `ICurrentUserAccessor.GetUserIdAsync()` e operano solo sui movimenti dei conti di quell'utente; un id altrui risponde 404.
- `POST /query` — ricerca paginata (`PagedResult<TransactionDto>`: `items`, `totalCount`, `page`, `pageSize`), filtrabile per `fromDate`/`toDate` (estremi inclusi), conti, categorie e `isIncome`. Ordinamento opzionale con `sortBy` (`date` default, `description`, `category`, `account`, `amount`) e `sortDirection` (`asc`/`desc`, default `desc`); valori sconosciuti ricadono su data decrescente.
- `GET /month-options` — mesi disponibili per i filtri UI.
- `GET /landing?excludeTransfers=true` — aggregazione dashboard (saldi, riepilogo categorie, serie net-worth).
- `GET /{id}` — transazione singola.
- `GET /latest` — transazione più recente.
- `POST /` — crea.
- `PUT /` — aggiorna.
- `DELETE /{id}` — elimina.
- `POST /series` — time-series per periodo (somme).
- `POST /stock` — time-series cumulativa.

## `TransferFunctions` — `api/transfer`
Come per le transazioni: user-scoped tramite `GetUserIdAsync()`, entrambi i conti devono appartenere all'utente.
- `GET /` — elenco trasferimenti.
- `GET /{id}` — trasferimento singolo.
- `POST /` — crea (due leg collegate).
- `PUT /{id}` — aggiorna (ricrea le due leg).
- `DELETE /{id}` — elimina trasferimento + leg.

## `DemoFunctions` — `api/demo`
- `POST /generate` — rigenera da zero i dati demo (account, transazioni, trasferimenti) per l'utente autenticato e ne ripristina le categorie di default (`CategoryEnum`). Fallisce con 403 se l'utente corrente non ha `IsDemo = true` sul record `User`. Vedi [07-application-services-and-mapping](../../02-data-persistence/tech/07-application-services-and-mapping.tech.md) per la logica di generazione.

## `ImportFunctions` (`[RequireRole("Files.Sender")]`) — `api/import`
Vedi [08-import-overview-and-dedup](../../03-import-pipeline/tech/08-import-overview-and-dedup.tech.md).
- `POST /excel`, `/excel/base64`, `/excel/stream` — import Excel legacy multi-sheet.
- `POST /bbva-csv`, `/sella-csv`, `/satispay-csv`, `/traderepublic-csv` — import CSV bancario (multipart, campo `file`, estensione `.csv`).
- `POST /{bank}-csv/stream` — variante raw octet-stream (`?fileName=` opzionale).
- `POST /{bank}-xlsx` — variante `.xlsx` raw, convertita in CSV in-memory prima dell'import. Un `Content-Type` diverso da XLSX restituisce 415.
- `POST /test-seed` (`Import_TestSeed`) — nessun body: elimina e rigenera conti/movimenti/trasferimenti fittizi dell'utente `test@dev.local` (categorie riportate a quelle di default), creandolo se non esiste (`ITestSeedService.ResetTestUserDataAsync`). Non usa l'identità del chiamante (nessun `GetUserIdForImportAsync`), ma resta soggetto a `[RequireRole("Files.Sender")]` come gli altri import. Passa da `FunctionBase.ExecuteAsync`: `200` con `TestSeedResultDto` (`userId`, `email`, `userCreated`, `accountCount`, `transactionCount`, `transferCount`, `fromDate`, `toDate`), errori come problem details. Ogni giorno lo stesso servizio è eseguito dalla timer function `Scheduled_DemoDataReset` (vedi sotto); l'endpoint resta per le esecuzioni manuali. Logica in [07-application-services-and-mapping](../../02-data-persistence/tech/07-application-services-and-mapping.tech.md).

- `POST /transfers/reconcile` (`Import_ReconcileTransfers`) — nessun body: cerca tra i movimenti già importati di **tutti gli utenti** le coppie che rappresentano un trasferimento (sempre tra conti dello stesso utente) e le collega (`ITransferReconciliationService.ReconcileAsync`). Come `Import_TestSeed` non usa l'identità del chiamante (nessun `GetUserIdForImportAsync`, quindi funziona anche con un token il cui oid non è associato a un utente), ma resta soggetto a `[RequireRole("Files.Sender")]`. Query opzionali `fromDate`, `toDate` (date ISO; `FunctionBase.GetQueryDate`, formato non valido → 400) e `dryRun` (default `false`: con `true` restituisce i match senza salvarli). `200` con `TransferReconciliationResultDto`. Logica in [10-transfer-matching](../../03-import-pipeline/tech/10-transfer-matching.tech.md).

Tutti gli endpoint di import di file accettano il flag opzionale `?importAll`. Le tre varianti (multipart, stream, xlsx) fanno solo la validazione HTTP (`ImportFormFileAsync` / `ImportRawBodyAsync` / `ImportXlsxAsCsvAsync`) e delegano a `IBankFileImportService.ImportAsync(source, format, …, logSource)` (Application), che sceglie il servizio della banca, converte gli XLSX delle banche in CSV e registra l'esito in `ImportLogs` (vedi [08-import-overview-and-dedup](../../03-import-pipeline/tech/08-import-overview-and-dedup.tech.md#tracciamento-degli-import-importlogs)); `/excel/base64` usa `IBankFileImportService.RunTrackedAsync`. Poi `ToImportResult` (`200 { importedCount }` oppure `400 { errors: [...] }`). La forma della risposta e le etichette in `ImportLogs.Source` (`"BBVA CSV stream"`, `"Sella XLSX"`, …) non cambiano. Lo stesso servizio è usato dall'import automatico da OneDrive (sotto).

## `OneDriveImportFunctions` — import automatico da OneDrive
Sostituisce la Logic App di import. Flusso: Microsoft Graph notifica una modifica nella cartella OneDrive osservata → la notifica viene accodata → la queue function importa i file nuovi. Dettagli in [08-import-overview-and-dedup](../../03-import-pipeline/tech/08-import-overview-and-dedup.tech.md#import-automatico-da-onedrive). Con `OneDriveImport:Enabled=false` (default) le tre function non fanno nulla.
- `POST /api/onedrive/notifications` (`OneDrive_Notifications`, `[AllowAnonymousFunction]`) — endpoint delle change notifications Graph, che non invia token: ogni notifica è verificata con il `clientState` condiviso (`IOneDriveImportService.IsValidClientState`, confronto a tempo costante), quelle non valide sono scartate. Con `?validationToken=` (validazione alla creazione della subscription) restituisce il token in `text/plain`. Altrimenti risponde subito `202` e, se almeno una notifica è valida, scrive **un** messaggio nella coda `onedrive-sync` (multi-output: `[HttpResult]` + `[QueueOutput]`).
- `OneDrive_Sync` — `QueueTrigger("onedrive-sync")` (pacchetto `Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues`, coda nello storage host, connessione identity-based `AzureWebJobsStorage`). Chiama `IOneDriveImportService.SyncAsync`; un `Result` fallito (es. un'altra sincronizzazione in corso) diventa eccezione e il messaggio viene ritentato. `host.json` → `extensions.queues`: `batchSize: 1`, `newBatchThreshold: 0`, `visibilityTimeout: 00:00:30`, `maxDequeueCount: 5` (poi `onedrive-sync-poison`).
- `OneDrive_SubscriptionRenewal` — `TimerTrigger("%OneDriveSubscriptionRenewalSchedule%")` (default `0 0 4 * * *`, UTC; in Azure dal parametro Bicep `oneDriveSubscriptionRenewalSchedule`). Chiama `IOneDriveImportService.EnsureSubscriptionAsync(notificationUrl)`: crea la baseline della delta query se manca, poi crea la subscription o la rinnova se scade entro 5 giorni. `notificationUrl` = `OneDriveImport:NotificationUrl` oppure `https://{WEBSITE_HOSTNAME}/api/onedrive/notifications`.
- In locale `local.settings.example.json` contiene `OneDriveSubscriptionRenewalSchedule` (obbligatorio per l'indicizzazione), `OneDriveImport__Enabled=false` e `AzureWebJobs.OneDrive_SubscriptionRenewal.Disabled=true`. La coda richiede Azurite (`UseDevelopmentStorage=true`). Esecuzione manuale: `POST http://localhost:7120/admin/functions/OneDrive_SubscriptionRenewal` (o `OneDrive_Sync`) con body `{}`.

## `ScheduledFunctions` — timer trigger (nessuna route)
- `Scheduled_DemoDataReset` — `TimerTrigger("%DemoResetSchedule%")` (pacchetto `Microsoft.Azure.Functions.Worker.Extensions.Timer`; espressione NCRONTAB in UTC risolta dall'host: app setting `DemoResetSchedule`, in Azure dal parametro Bicep `demoResetSchedule`, default `0 0 3 * * *`). Esegue **in sequenza** `ITestSeedService.ResetTestUserDataAsync()` e poi `ITransferReconciliationService.ReconcileAsync(null, null, dryRun: false)`, cioè quello che faceva la Logic App di reset demo chiamando `Import_TestSeed` e `Import_ReconcileTransfers` (che lì partivano in parallelo). `RunOnStartup = false`, `UseMonitor = true` (un'esecuzione saltata parte al riavvio successivo).
- Non eredita da `FunctionBase` e non passa per l'autenticazione: i middleware saltano le invocazioni senza `HttpContext`. Un `Result` fallito diventa `InvalidOperationException`, quindi l'invocazione risulta fallita in Application Insights.
- In locale `local.settings.example.json` contiene `DemoResetSchedule` (obbligatorio: senza, la function non si indicizza) e `AzureWebJobs.Scheduled_DemoDataReset.Disabled=true`, per non toccare il DB configurato con `func start`. Per lanciarla a mano: togliere il flag e chiamare `POST http://localhost:7120/admin/functions/Scheduled_DemoDataReset` con body `{}`.

## OpenAPI
Con la migrazione ad Azure Functions la UI Scalar (`/scalar`) e il documento OpenAPI built-in non sono più esposti.

## Collection Postman
In `tools/postman/ExpenseTracker.postman_collection.json` c'è una collection Postman (schema v2.1) con tutte le function sopra elencate, organizzate per classe. Usa le variabili di collection `baseUrl` (default `http://localhost:7120/api`), `accessToken` (Bearer applicato a livello di collection), `accountId`/`toAccountId`/`categoryId`/`transactionId`/`transferId` e `importAll`. Accanto c'è l'environment `ExpenseTracker.Local.postman_environment.json` con gli stessi valori per l'esecuzione locale (`accessToken` è di tipo secret e va valorizzato a mano). Quando si aggiunge o modifica un endpoint, va aggiornata anche la collection.
