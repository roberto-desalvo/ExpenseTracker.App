# 11 - Testing (Tecnico)

## Progetto
`RDS.ExpenseTracker.Tests` — xUnit + FluentAssertions. Struttura piatta, un file per importer:
- `BbvaCsvImportServiceTests.cs`
- `SellaCsvImportServiceTests.cs`
- `SatisPayCsvImportServiceTests.cs`
- `TradeRepublicCsvImportServiceTests.cs`

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
