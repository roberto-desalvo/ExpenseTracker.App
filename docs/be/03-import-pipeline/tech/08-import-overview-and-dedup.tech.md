# 08 - Import overview and dedup (Tecnico)

## Entry point
`ImportFunctions` (Azure Functions HTTP-triggered, `Functions/Functions/ImportFunctions.cs`) è un thin adapter su quattro importer CSV per banca più l'importer Excel legacy. Ogni servizio CSV implementa `ImportFromCsvAsync(Stream, fileName, int userId, importAll = false, ImportStatistics? statistics = null) : Task<Result<int>>` (`IExcelImportService` analogamente su `ImportFromExcelAsync`/`ImportFromExcelBase64Async`); `statistics` è il collettore usato dal tracciamento degli import (vedi sotto). `userId` è risolto da `ICurrentUserAccessor.GetUserIdForImportAsync()` in ciascuna delle 15 function di import (quattro banche × multipart/octet-stream/xlsx, più le tre varianti Excel multipart/base64/stream), tramite le pipeline condivise `ImportFormFileAsync`/`ImportRawBodyAsync`/`ImportXlsxAsCsvAsync`, e passato a valle. Gli script PowerShell in `scripts/` cambiano solo nell'URL base (in locale `http://localhost:7120`): l'identità arriva comunque dal bearer token Azure AD, risolta interamente server-side.

## Flusso comune
1. **Upload** — multipart upload, body raw octet-stream, oppure (endpoint `*-xlsx`) body `.xlsx` raw trascodificato in CSV in-memory via `ExcelDataReader`.
2. **Parse** — parser dedicato per banca, vedi [09-bank-specific-importers](09-bank-specific-importers.tech.md).
3. **Deduplicazione** — ogni riga riceve un `ExternalId` naturale (BBVA: fingerprint SHA-256 dei campi; le altre banche: id riga/transazione nativo). La dedup è **per utente**: le righe con un `ExternalId` già presente tra i movimenti dei conti dell'utente che importa vengono filtrate prima dell'insert (`ITransactionRepository.GetExistingExternalIds(externalIds, userId)`; per i trasferimenti Sella `ITransferRepository.GetTransferByExternalId(externalId, userId)`), così utenti diversi possono importare lo stesso movimento. Backstop a livello DB: indice univoco filtrato su `Transactions (AccountId, ExternalId)`; `Transfers.ExternalId` ha un indice non univoco (vedi [06-database-and-ef-core](../../02-data-persistence/tech/06-database-and-ef-core.tech.md)).
4. **Map & enrich** — account mancanti auto-creati **per l'utente corrente** (`EnsureAccountsExistAsync(names, userId)`, filtra/crea sempre `Account` con quello specifico `UserId`); categorie assegnate per default (`Default`/`MoneyTransfers`) o per tag-matching sulla descrizione (BBVA, Trade Republic); Trade Republic forza `SavingsAndInvestments` per i tipi riga d'investimento.
5. **Transfer matching** (Sella, Satispay, Trade Republic — non BBVA), anch'esso scoped per `userId` — vedi [10-transfer-matching](10-transfer-matching.tech.md).
6. **Persist** — insert bulk di transazioni e trasferimenti in un'unica `SaveChangesAsync`; ritorna il conteggio righe importate.
7. **Tracciamento** — l'esito viene registrato in `ImportLogs` (sotto).

Il conto principale dell'importer viene risolto (ed eventualmente creato) subito dopo il parse, **prima** della dedup: così anche un file composto solo da righe già importate registra il conto nel log. Un file vuoto (nessuna riga) termina prima e non crea conti.

## Tracciamento degli import (`ImportLogs`)
Ogni flusso di import di file — successo, `Result.Fail` o eccezione — produce una riga in `ImportLogs` (entità `ImportLog`, vedi [06-database-and-ef-core](../../02-data-persistence/tech/06-database-and-ef-core.tech.md)). Le richieste rifiutate prima di chiamare il servizio (file mancante, estensione errata, 415) non vengono registrate.

- **Raccolta** — `ImportFunctions.RunTrackedImportAsync` crea un `ImportStatistics` (`Domain/Models/DataImport/`) e lo passa al servizio, che lo compila man mano; le statistiche parziali restano disponibili anche se l'import fallisce. Per gli endpoint `*-xlsx` la conversione XLSX→CSV avviene dentro il flusso tracciato, così un file illeggibile risulta come import fallito.
- **Scrittura** — `IImportLogService.RecordAsync(source, fileName, userId, statistics, errors)` (`ImportLogService`): `Source` è l'etichetta dell'endpoint (es. `"BBVA CSV"`, `"Sella XLSX"`, `"Excel base64"`), gli errori sono uniti con ` | ` e troncati a 4000 caratteri. Un errore di scrittura del log viene loggato e **non** fa fallire l'import. In caso di eccezione il log viene scritto e l'eccezione rilanciata al middleware.
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

## Seed dei dati di test
`ImportFunctions` espone anche `Import_TestSeed` (`POST api/import/test-seed`), che non ha file in input e non segue il flusso sopra: delega a `ITestSeedService.ResetTestUserDataAsync()`, che cancella e rigenera i dati fittizi dell'utente `test@dev.local` (creato se assente) indipendentemente dall'identità del chiamante. Condivide con gli altri import solo il requisito del ruolo `Files.Sender`. Nessuna dedup o transfer matching: i giroconti sono generati già collegati. Dettagli in [07-application-services-and-mapping](../../02-data-persistence/tech/07-application-services-and-mapping.tech.md).

## Legacy Excel importer
`ExcelImportService` parsa un workbook multi-sheet (un foglio per mese) con indici colonna configurabili (`ExpenseTrackerExcel` options), auto-crea account per l'utente corrente, tag-matcha categorie, filtro opzionale sulle sole righe più recenti dell'ultima transazione esistente.

## Endpoint
Vedi [03-api-surface-and-routing](../../01-platform/tech/03-api-surface-and-routing.tech.md#importfunctions-requirerolefilessender--apiimport).
