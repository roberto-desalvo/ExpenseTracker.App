# 08 - Import overview and dedup (Tecnico)

## Entry point
`ImportController` è un thin adapter su quattro importer CSV per banca più l'importer Excel legacy. Ogni servizio CSV implementa `ImportFromCsvAsync(Stream, fileName, importAll = false) : Task<Result<int>>`.

## Flusso comune
1. **Upload** — multipart upload, body raw octet-stream, oppure (endpoint `*-xlsx`) body `.xlsx` raw trascodificato in CSV in-memory via `ExcelDataReader`.
2. **Parse** — parser dedicato per banca, vedi [09-bank-specific-importers](09-bank-specific-importers.tech.md).
3. **Deduplicazione** — ogni riga riceve un `ExternalId` naturale (BBVA: fingerprint SHA-256 dei campi; le altre banche: id riga/transazione nativo). Le righe con `ExternalId` già presente vengono filtrate prima dell'insert (`ITransactionRepository.GetExistingExternalIds`), con backstop a livello DB dall'indice univoco filtrato su `Transactions.ExternalId`/`Transfers.ExternalId` (vedi [06-database-and-ef-core](../../02-data-persistence/tech/06-database-and-ef-core.tech.md)).
4. **Map & enrich** — account mancanti auto-creati; categorie assegnate per default (`Default`/`MoneyTransfers`) o per tag-matching sulla descrizione (BBVA, Trade Republic); Trade Republic forza `SavingsAndInvestments` per i tipi riga d'investimento.
5. **Transfer matching** (Sella, Satispay, Trade Republic — non BBVA) — vedi [10-transfer-matching](10-transfer-matching.tech.md).
6. **Persist** — insert bulk di transazioni e trasferimenti in un'unica `SaveChangesAsync`; ritorna il conteggio righe importate.

## Legacy Excel importer
`ExcelImportService` parsa un workbook multi-sheet (un foglio per mese) con indici colonna configurabili (`ExpenseTrackerExcel` options), auto-crea account, tag-matcha categorie, filtro opzionale sulle sole righe più recenti dell'ultima transazione esistente.

## Endpoint
Vedi [03-api-surface-and-routing](../../01-platform/tech/03-api-surface-and-routing.tech.md#importcontroller-authorizeroles--filessender--apiimport).
