# 08 - Import overview and dedup (Tecnico)

## Entry point
Due ingressi condividono lo stesso servizio applicativo `IBankFileImportService` (`Application/Services/BankFileImportService.cs`):
- `ImportFunctions` (HTTP, `Functions/Functions/ImportFunctions.cs`), per caricamenti manuali e script;
- l'import automatico da OneDrive (`OneDriveImportFunctions` → `IOneDriveImportService`, sotto).

`IBankFileImportService.ImportAsync(BankFileSource, BankFileFormat, Stream, fileName, userId, importAll, logSource)` instrada il file a uno dei quattro importer CSV per banca o all'importer Excel legacy (`BankFileSource.Excel`, il formato è ignorato), converte prima in CSV gli `.xlsx` delle banche (`BankFileFormat.Xlsx`; delimitatore `;` per Satispay, `,` per le altre) e registra l'esito in `ImportLogs`. Ogni servizio CSV implementa `ImportFromCsvAsync(Stream, fileName, int userId, importAll = false, ImportStatistics? statistics = null) : Task<Result<int>>` (`IExcelImportService` analogamente su `ImportFromExcelAsync`/`ImportFromExcelBase64Async`); `statistics` è il collettore usato dal tracciamento degli import (vedi sotto). `userId` è risolto da `ICurrentUserAccessor.GetUserIdForImportAsync()` in ciascuna delle 15 function di import (quattro banche × multipart/octet-stream/xlsx, più le tre varianti Excel multipart/base64/stream), tramite le pipeline condivise `ImportFormFileAsync`/`ImportRawBodyAsync`/`ImportXlsxAsCsvAsync` (solo validazione HTTP), e passato a `IBankFileImportService`. Gli script PowerShell in `scripts/` cambiano solo nell'URL base (in locale `http://localhost:7120`): l'identità arriva comunque dal bearer token Azure AD, risolta interamente server-side.

## Flusso comune
1. **Upload** — multipart upload, body raw octet-stream, (endpoint `*-xlsx`) body `.xlsx` raw, oppure file scaricato da OneDrive. Gli `.xlsx` delle banche sono trascodificati in CSV in-memory via `ExcelDataReader` (`BankFileImportService.ConvertExcelToCsv`).
2. **Parse** — parser dedicato per banca, vedi [09-bank-specific-importers](09-bank-specific-importers.tech.md).
3. **Deduplicazione** — ogni riga riceve un `ExternalId` naturale (BBVA: fingerprint SHA-256 dei campi; le altre banche: id riga/transazione nativo). La dedup è **per utente**: le righe con un `ExternalId` già presente tra i movimenti dei conti dell'utente che importa vengono filtrate prima dell'insert (`ITransactionRepository.GetExistingExternalIds(externalIds, userId)`; per i trasferimenti Sella `ITransferRepository.GetTransferByExternalId(externalId, userId)`), così utenti diversi possono importare lo stesso movimento. Backstop a livello DB: indice univoco filtrato su `Transactions (AccountId, ExternalId)`; `Transfers.ExternalId` ha un indice non univoco (vedi [06-database-and-ef-core](../../02-data-persistence/tech/06-database-and-ef-core.tech.md)).
4. **Map & enrich** — account mancanti auto-creati **per l'utente corrente** (`EnsureAccountsExistAsync(names, userId)`, filtra/crea sempre `Account` con quello specifico `UserId`); categorie **dell'utente corrente** (`UserCategories` costruito da `ICategoryRepository.GetCategories(userId)`) assegnate per default (la categoria `IsDefault`/`Kind = Default` e quella `Kind = MoneyTransfers` dell'utente; `null` se l'utente non ce l'ha) o per tag-matching sulla descrizione (BBVA, Trade Republic, Excel) tramite il `CategoryTagMatcher` condiviso, lo stesso usato da `POST api/category/sync` per riclassificare i movimenti già salvati; Trade Republic forza la categoria `SavingsAndInvestments` dell'utente per i tipi riga d'investimento.
5. **Transfer matching** (Sella, Satispay, Trade Republic — non BBVA), anch'esso scoped per `userId` — vedi [10-transfer-matching](10-transfer-matching.tech.md).
6. **Persist** — insert bulk di transazioni e trasferimenti in un'unica `SaveChangesAsync`; ritorna il conteggio righe importate.
7. **Tracciamento** — l'esito viene registrato in `ImportLogs` (sotto).

Il conto principale dell'importer viene risolto (ed eventualmente creato) subito dopo il parse, **prima** della dedup: così anche un file composto solo da righe già importate registra il conto nel log. Un file vuoto (nessuna riga) termina prima e non crea conti.

## Tracciamento degli import (`ImportLogs`)
Ogni flusso di import di file — successo, `Result.Fail` o eccezione — produce una riga in `ImportLogs` (entità `ImportLog`, vedi [06-database-and-ef-core](../../02-data-persistence/tech/06-database-and-ef-core.tech.md)). Le richieste rifiutate prima di chiamare il servizio (file mancante, estensione errata, 415) non vengono registrate.

- **Raccolta** — `IBankFileImportService.RunTrackedAsync` (chiamato da `ImportAsync`) crea un `ImportStatistics` (`Domain/Models/DataImport/`) e lo passa al servizio, che lo compila man mano; le statistiche parziali restano disponibili anche se l'import fallisce. Per gli `.xlsx` delle banche la conversione XLSX→CSV avviene dentro il flusso tracciato, così un file illeggibile risulta come import fallito.
- **Scrittura** — `IImportLogService.RecordAsync(source, fileName, userId, statistics, errors)` (`ImportLogService`): `Source` è l'etichetta dell'endpoint (es. `"BBVA CSV"`, `"BBVA CSV stream"`, `"Sella XLSX"`, `"Excel base64"`) oppure, per OneDrive, quella di default con suffisso (`"BBVA CSV (OneDrive)"`, `"Excel (OneDrive)"`); `FileName` è il nome reale del file (la Logic App inviava sempre il default, es. `bbva.csv`), gli errori sono uniti con ` | ` e troncati a 4000 caratteri. Un errore di scrittura del log viene loggato e **non** fa fallire l'import. In caso di eccezione il log viene scritto e l'eccezione rilanciata al middleware.
- **Contesto condiviso** — `ImportLogRepository.SaveImportLog` esegue `ChangeTracker.Clear()` prima di aggiungere il log: un import fallito può lasciare entità aggiunte e non salvate nel `DbContext` scoped, che altrimenti verrebbero salvate insieme al log.

| Campo | Significato per importer |
|---|---|
| `AccountId` | Conto principale dell'importer (BBVA, Sella, Satispay; Trade Republic: il conto di default, anche se il file tocca il conto trading). Excel: valorizzato solo se il file riguarda un unico conto, altrimenti `null`. |
| `MinTransactionDate` / `MaxTransactionDate` | Periodo coperto dalle righe valide del file, **comprese** quelle scartate come duplicate (Satispay: escluse le righe annullate; Trade Republic: escluse le righe a importo zero; Excel: date normalizzate sull'anno del foglio). |
| `NewTransactionsCount` | Movimenti inseriti, incluse entrambe le leg dei trasferimenti (coincide con `importedCount` della risposta). |
| `NewTransfersCount` | `Transfer` creati, inclusi quelli che collegano una leg nuova a un movimento esistente tramite transfer matching. |
| `SkippedDuplicatesCount` | Righe scartate perché già importate: `ExternalId` già presente; per Sella anche i codici ripetuti nello stesso file e i trasferimenti già esistenti (per `ExternalId` o per conti/importo/data); per Excel le righe filtrate da `FilterSheetsByLatestTransactionAsync`. Con `importAll=true` la dedup (salvo Satispay, sempre attiva) non avviene e il valore resta 0. |
| `ErrorMessage` | Messaggi di errore del `Result` o dell'eccezione; `null` se l'import è riuscito. |

Nessun endpoint espone ancora i log: si consultano direttamente in tabella.

## Import automatico da OneDrive
Sostituisce la Logic App di import (che controllava la cartella una volta al giorno). Function in [03-api-surface-and-routing](../../01-platform/tech/03-api-surface-and-routing.tech.md#onedriveimportfunctions--import-automatico-da-onedrive), autenticazione verso Graph in [04-auth-and-request-pipeline](../../01-platform/tech/04-auth-and-request-pipeline.tech.md#import-automatico-da-onedrive--nessun-principal), infrastruttura in [13-infrastructure-as-code](../../04-operations/tech/13-infrastructure-as-code.tech.md).

**Convenzione delle cartelle** (`OneDriveImport:FolderId` = `/Transazioni`): `/Transazioni/<Banca>/file`. `OneDriveImportPathResolver` mappa il nome della sottocartella (case-insensitive) a `BankFileSource`: `BBVA`, `Sella`, `Satispay`, `Trade_Republic` (anche `Trade Republic`), `Excel`; e l'estensione a `BankFileFormat`: banche `.csv`/`.xlsx`, `Excel` solo `.xlsx`. La cartella `Excel` ora usa l'importer Excel (la Logic App chiamava route inesistenti).

**`IOneDriveImportService.SyncAsync`** (`Application/Services/OneDriveImportService.cs`):
1. Acquisisce il lock: `IOneDriveSyncStateRepository.TryAcquireLockAsync` è un `UPDATE … WHERE LockedUntil IS NULL OR LockedUntil < now` sulla riga singola `OneDriveSyncStates` (scadenza 10 minuti). Se è occupato → `Result.Fail` (`onedrive.sync_in_progress`) e il messaggio in coda viene ritentato. Il lock è rilasciato in `finally`.
2. Senza `DeltaLink` salvato crea solo la **baseline** (`delta?token=latest`): i file già presenti non vengono importati.
3. Altrimenti esegue la delta query dal `DeltaLink` (`IOneDriveClient.GetDeltaAsync`, segue `@odata.nextLink`). Un `410 Gone` (`OneDriveDeltaExpiredException`) → nuova baseline, con warning.
4. Considera solo i file non eliminati il cui `parentReference.id` è una **sottocartella banca diretta** della radice (elencate con `GetChildFoldersAsync`): file nella radice, in `Importati`/`Errori` o in cartelle non previste sono ignorati. La delta non restituisce `parentReference.path`, per questo si confrontano gli id.
5. Risolve l'utente proprietario da `OneDriveImport:TargetUserEmail` (`IUserRepository.GetByEmail`). Se manca → `Result.Fail` **senza** avanzare il `DeltaLink`.
6. Per ogni file: estensione non supportata → spostato in `Errori`; altrimenti download (`/content`), `IBankFileImportService.ImportAsync(..., importAll: false)`, poi spostamento in `<Banca>/Importati` se il `Result` è riuscito, in `<Banca>/Errori` se è fallito o ha sollevato un'eccezione (nomi da `OneDriveImport:ImportedFolderName`/`FailedFolderName`; cartelle create al bisogno; in caso di nome già presente Graph rinomina, `conflictBehavior=rename`). Un errore di **download** o di spostamento interrompe la sincronizzazione senza avanzare il `DeltaLink`: al nuovo tentativo il file viene rielaborato (la dedup evita doppioni).
7. Salva il nuovo `DeltaLink` (`UpdateDeltaLinkAsync`). Gli spostamenti generano a loro volta notifiche, che producono delta senza file da importare.

Lo stato è aggiornato con `ExecuteUpdateAsync` e non con entità tracciate, perché `ImportLogRepository.SaveImportLog` svuota il change tracker condiviso.

**`EnsureSubscriptionAsync`** (timer giornaliero): crea la baseline se manca (prima che arrivino notifiche); se la subscription salvata scade tra più di 5 giorni non fa nulla, altrimenti la rinnova (`PATCH /subscriptions/{id}`, +41000 minuti, sotto il massimo Graph di 42300) o, se non esiste più (404), ne crea una nuova su `OneDriveImport:SubscriptionResource` (default `/me/drive/items/{FolderId}`) con `changeType=updated` e il `clientState`.

**Client Graph** — `GraphOneDriveClient` (Infrastructure, `HttpClient` tipizzato su `https://graph.microsoft.com/v1.0/`, niente SDK Graph) e `OneDriveTokenProvider`, registrati da `AddOneDriveServices()`.

## Seed dei dati di test
`ImportFunctions` espone anche `Import_TestSeed` (`POST api/import/test-seed`), che non ha file in input e non segue il flusso sopra: delega a `ITestSeedService.ResetTestUserDataAsync()`, che cancella e rigenera i dati fittizi dell'utente `test@dev.local` (creato se assente), categorie di default comprese, indipendentemente dall'identità del chiamante. Condivide con gli altri import solo il requisito del ruolo `Files.Sender`. Nessuna dedup o transfer matching: i giroconti sono generati già collegati. Lo stesso servizio gira ogni giorno, senza HTTP, nella timer function `Scheduled_DemoDataReset` (seguito da `ReconcileAsync`; vedi [03-api-surface-and-routing](../../01-platform/tech/03-api-surface-and-routing.tech.md#scheduledfunctions--timer-trigger-nessuna-route)). Dettagli in [07-application-services-and-mapping](../../02-data-persistence/tech/07-application-services-and-mapping.tech.md).

## Legacy Excel importer
`ExcelImportService` parsa un workbook multi-sheet (un foglio per mese) con indici colonna configurabili (`ExpenseTrackerExcel` options), auto-crea account per l'utente corrente, tag-matcha categorie, filtro opzionale sulle sole righe più recenti dell'ultima transazione esistente.

## Endpoint
Vedi [03-api-surface-and-routing](../../01-platform/tech/03-api-surface-and-routing.tech.md#importfunctions-requirerolefilessender--apiimport).
