# 08 - Import overview and dedup (Tecnico)

## Entry point
`ImportFunctions` (Azure Functions HTTP-triggered, `Functions/Functions/ImportFunctions.cs`) è un thin adapter su quattro importer CSV per banca più l'importer Excel legacy. Ogni servizio CSV implementa `ImportFromCsvAsync(Stream, fileName, int userId, importAll = false) : Task<Result<int>>` (`IExcelImportService` analogamente su `ImportFromExcelAsync`/`ImportFromExcelBase64Async`). `userId` è risolto da `ICurrentUserAccessor.GetUserIdForImportAsync()` in ciascuna delle 15 function di import (quattro banche × multipart/octet-stream/xlsx, più le tre varianti Excel multipart/base64/stream), tramite le pipeline condivise `ImportFormFileAsync`/`ImportRawBodyAsync`/`ImportXlsxAsCsvAsync`, e passato a valle. Gli script PowerShell in `scripts/` cambiano solo nell'URL base (in locale `http://localhost:7120`): l'identità arriva comunque dal bearer token Azure AD, risolta interamente server-side.

## Flusso comune
1. **Upload** — multipart upload, body raw octet-stream, oppure (endpoint `*-xlsx`) body `.xlsx` raw trascodificato in CSV in-memory via `ExcelDataReader`.
2. **Parse** — parser dedicato per banca, vedi [09-bank-specific-importers](09-bank-specific-importers.tech.md).
3. **Deduplicazione** — ogni riga riceve un `ExternalId` naturale (BBVA: fingerprint SHA-256 dei campi; le altre banche: id riga/transazione nativo). La dedup è **per utente**: le righe con un `ExternalId` già presente tra i movimenti dei conti dell'utente che importa vengono filtrate prima dell'insert (`ITransactionRepository.GetExistingExternalIds(externalIds, userId)`; per i trasferimenti Sella `ITransferRepository.GetTransferByExternalId(externalId, userId)`), così utenti diversi possono importare lo stesso movimento. Backstop a livello DB: indice univoco filtrato su `Transactions (AccountId, ExternalId)`; `Transfers.ExternalId` ha un indice non univoco (vedi [06-database-and-ef-core](../../02-data-persistence/tech/06-database-and-ef-core.tech.md)).
4. **Map & enrich** — account mancanti auto-creati **per l'utente corrente** (`EnsureAccountsExistAsync(names, userId)`, filtra/crea sempre `Account` con quello specifico `UserId`); categorie assegnate per default (`Default`/`MoneyTransfers`) o per tag-matching sulla descrizione (BBVA, Trade Republic); Trade Republic forza `SavingsAndInvestments` per i tipi riga d'investimento.
5. **Transfer matching** (Sella, Satispay, Trade Republic — non BBVA), anch'esso scoped per `userId` — vedi [10-transfer-matching](10-transfer-matching.tech.md).
6. **Persist** — insert bulk di transazioni e trasferimenti in un'unica `SaveChangesAsync`; ritorna il conteggio righe importate.

## Seed dei dati di test
`ImportFunctions` espone anche `Import_TestSeed` (`POST api/import/test-seed`), che non ha file in input e non segue il flusso sopra: delega a `ITestSeedService.ResetTestUserDataAsync()`, che cancella e rigenera i dati fittizi dell'utente `test@dev.local` (creato se assente) indipendentemente dall'identità del chiamante. Condivide con gli altri import solo il requisito del ruolo `Files.Sender`. Nessuna dedup o transfer matching: i giroconti sono generati già collegati. Dettagli in [07-application-services-and-mapping](../../02-data-persistence/tech/07-application-services-and-mapping.tech.md).

## Legacy Excel importer
`ExcelImportService` parsa un workbook multi-sheet (un foglio per mese) con indici colonna configurabili (`ExpenseTrackerExcel` options), auto-crea account per l'utente corrente, tag-matcha categorie, filtro opzionale sulle sole righe più recenti dell'ultima transazione esistente.

## Endpoint
Vedi [03-api-surface-and-routing](../../01-platform/tech/03-api-surface-and-routing.tech.md#importfunctions-requirerolefilessender--apiimport).
