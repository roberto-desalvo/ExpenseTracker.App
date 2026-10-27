# 11 - Testing (Tecnico)

## Progetto
`RDS.ExpenseTracker.Tests` — xUnit + FluentAssertions. Struttura piatta, un file per importer:
- `BbvaCsvImportServiceTests.cs`
- `SellaCsvImportServiceTests.cs` — include la verifica di `ImportStatistics` (conto, periodo, nuovi movimenti/trasferimenti, duplicati da DB e nello stesso file, file con sole righe duplicate).
- `ImportLogServiceTests.cs` — mappatura delle statistiche su `ImportLog`, unione e troncamento degli errori, nessuna eccezione se il repository fallisce.
- `SatisPayCsvImportServiceTests.cs`
- `TradeRepublicCsvImportServiceTests.cs`
- `TransferReconciliationServiceTests.cs` — riconciliazione trasferimenti: match per regola, per IBAN del conto e per categoria `MoneyTransfers`, salto dei casi ambigui, tolleranza di 3 giorni, nessun match tra utenti diversi, `dryRun` senza modifiche, validazione dell'intervallo date, e un test di volume (80.000 movimenti, 20.000 coppie) con tetto di 10 secondi per intercettare regressioni quadratiche. Usa un `FakeReconciliationRepository` che registra le coppie passate a `LinkTransfers`.

## Approccio
Nessun mocking framework: ogni file di test definisce le proprie fake in-file (`FakeTransactionRepository`, `FakeAccountRepository`, `FakeCategoryRepository`, `FakeTransferRepository`) che implementano direttamente le interfacce repository del Domain, più fake options (`FakeBbvaCsvOptions`, ecc.) per le interfacce `I*CsvOptions`. Nessuna infrastruttura di test condivisa: ogni classe è autosufficiente.

## Copertura
Parsing CSV (header, formati data/decimale italiani, simboli valuta), deduplicazione via `ExternalId`, applicazione delle regole di transfer matching, percorsi di errore/validazione (es. righe Sella senza `Codice identificativo` → `Result.Fail`), test di regressione su file di export reali cercati risalendo le directory (`FindFileUpwards("transazioni-bbva.csv")`).

## ArchUnitNET
Il `.csproj` referenzia `TngTech.ArchUnitNET`/`.xUnit`/`.xUnitV3` ma **non esistono ancora test di architettura scritti** — è scaffolding predisposto per future regole di conformità Clean Architecture, non ancora implementate.

## Comandi
```powershell
dotnet test ./api/RDS.ExpenseTracker.Tests/RDS.ExpenseTracker.Tests.csproj
dotnet test ./api/RDS.ExpenseTracker.Tests/RDS.ExpenseTracker.Tests.csproj --filter "FullyQualifiedName~TestName"
```
