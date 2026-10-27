# 02 - Domain model (Tecnico)

## Entità (`Domain/Entities/`)
- **`User`** — `Id`, `AzureOid?` (oid Azure AD dell'utente umano, nullable, indice univoco filtrato), `AppOid?` (oid dell'app/managed identity associata manualmente a questo utente, stesso pattern di indice — usato solo dal fallback JIT dell'import, vedi [04-auth-and-request-pipeline](../../01-platform/tech/04-auth-and-request-pipeline.tech.md)), `Email` (required), `IsDemo`; 1-N `Account`. Rappresenta l'utente interno, creato via JIT provisioning al primo accesso.
- **`Account`** — `Id`, `Name`, `UserId` (FK required verso `User`) + navigation `UserNavigation`, `Iban?` (normalizzato maiuscolo senza spazi, univoco per utente; usato da import e riconciliazione per riconoscere i trasferimenti, vedi [10-transfer-matching](../../03-import-pipeline/tech/10-transfer-matching.tech.md)); 1-N `Transaction`. Costruttore `Account(int id, string name, int userId)`.
- **`Category`** — `Id`, `Name`, `Description?`, `Priority?`, `IsDefault?`, `Tags?` (keyword list per auto-categorizzazione); 1-N `Transaction`. Seed via `Category.CreateSeedCategories()` + `Application.Builders.CategorySeedBuilder`.
- **`Transaction`** — record primario: `Amount`, `Description`, `Date?`, FK richiesta `AccountId`, FK opzionali `CategoryId`/`TransferId`, `ExternalId?` (fingerprint dedup import), `CreatedOn`/`UpdatedOn?`.
- **`Transfer`** — collega le due `Transaction` (leg) di un trasferimento; espone `TransactionFroms`/`TransactionTos` calcolati dal segno dell'importo.

- **`ImportLog`** — traccia di un flusso di import di file: `ImportedOn` (UTC), `UserId` (FK required), `Source`, `FileName?`, `AccountId?` (FK opzionale), `MinTransactionDate?`/`MaxTransactionDate?`, `NewTransactionsCount`, `NewTransfersCount`, `SkippedDuplicatesCount`, `ErrorMessage?`. Semantica dei campi in [08-import-overview-and-dedup](../../03-import-pipeline/tech/08-import-overview-and-dedup.tech.md#tracciamento-degli-import-importlogs).

Relazioni: `User` 1-N `ImportLog` (delete Cascade), `Account` 1-N `ImportLog` (delete SetNull), `User` 1-N `Account` (delete Restrict), `Account` 1-N `Transaction` (delete Restrict), `Category` 1-N `Transaction` (delete SetNull), `Transfer` 1-N `Transaction` (2 leg, delete SetNull).

## Enum (`Domain/Enums/`)
- `CategoryEnum` — Default, MoneyTransfers, WorkIncomes, Housing, HealthAndFitness, FoodAndBeverage, Transportation, Entertainment, Clothes, SavingsAndInvestments, Gifts.
- `TimeGranularityEnum` — Daily, Weekly, Monthly, Yearly.
- `DomainErrorKind` — BadRequest, Validation, Unauthorized, Forbidden, NotFound, Conflict (vedi [05-error-handling](../../01-platform/tech/05-error-handling.tech.md)).

> `AccountEnum` (seed statico di 8 account: Contanti, Hype, Satispay, Trade Republic, Sella, BBVA, PayPal, Trade Republic Trading) è stato **rimosso**: gli account sono ora sempre creati per-utente in modo lazy/JIT al primo import (`EnsureAccountsExistAsync` in ciascun importer), non più seedati via `HasData`.

## DTO (`Domain/Dtos/`, `Domain/Dtos/Requests/`)
Risposta: `UserDto`, `AccountDto` (include `Iban?` e `UserId`, quest'ultimo non impostabile dal client — vedi [07-application-services-and-mapping](../../02-data-persistence/tech/07-application-services-and-mapping.tech.md)), `CategoryDto`, `TransactionDto`, `TransferDto`, `PagedResult<T>`, `TransactionQueryResult` (paginazione + totali income/outcome/net), `TransactionMonthOptionDto`, `TimeSeriesDto`/`TimeSeriesListDto`/`TimeSeriesPointDto`/`TimeSeriesDimensionDto`, `LandingDashboardDto` (+ Account/Category/Totals), `TransferReconciliationResultDto`/`TransferReconciliationPairDto` (esito della riconciliazione trasferimenti).

Richiesta: `PagedQueryRequest` (base astratta: `Page`, `PageSize`), `AccountQueryRequest`, `CategoryQueryRequest`, `TransactionQueryRequest`, `TimeSeriesRequestDto`, `ImportExcelBase64Request`.

## Interfacce repository/service (`Domain/Repositories/`, `Domain/Services/`)
- `IUserRepository`, `IAccountRepository`, `ICategoryRepository`, `ITransferRepository`, `ITransactionRepository`, `ITransferReconciliationRepository` — implementate in Infrastructure. `ITransferReconciliationRepository` è l'unico accesso non filtrato per utente: serve alla riconciliazione massiva dei trasferimenti (vedi [10-transfer-matching](../../03-import-pipeline/tech/10-transfer-matching.tech.md)). `IAccountRepository` espone solo letture filtrate per utente (`GetAccount(id, userId)`, `GetAccounts(userId)`, `GetPagedAccounts(request, userId)`, usati da `AccountService`, `TransactionService`, `TransferService` e dalla pipeline di import). Le letture di `ITransactionRepository` (inclusa la dedup `GetExistingExternalIds`) e `GetTransfer`/`GetTransfers`/`GetTransferByExternalId` di `ITransferRepository` ricevono `userId` e restituiscono solo dati di conti dell'utente; `ITransactionService`/`ITransferService` ricevono `userId` su ogni metodo.
- `IUserService`, `IAccountService`, `ICategoryService`, `ITransactionService`, `ITransferService` — implementate in Application, ritornano `FluentResults.Result`/`Result<T>`.
- `ICurrentUserAccessor` — `GetUserIdAsync()` (usato da `AccountFunctions`/`AuthFunctions`/`TransactionFunctions`/`TransferFunctions`/`DemoFunctions`, richiede sempre email) e `GetUserIdForImportAsync()` (usato solo da `ImportFunctions`, tollera l'assenza di email e fa fallback sull'`AppOid`), vedi [04-auth-and-request-pipeline](../../01-platform/tech/04-auth-and-request-pipeline.tech.md).
- `IImportLogRepository` (`SaveImportLog`) / `IImportLogService` (`RecordAsync`) — registrazione degli import in `ImportLogs`; il modello `ImportStatistics` (`Domain/Models/DataImport/`) è il collettore compilato dagli importer.
- `IExcelImportService`, `IBbvaCsvImportService`, `ISellaCsvImportService`, `ISatisPayCsvImportService`, `ITradeRepublicCsvImportService`, `ITransferMatchingService`, `ITransferReconciliationService` — vedi modulo [08-import-overview-and-dedup](../../03-import-pipeline/tech/08-import-overview-and-dedup.tech.md).

## Contratti cross-cutting (`Domain/Common/`)
- `IAppOptions`, `IService`, `IRepository` — marker interfaces per DI reflection-based.
- `IExpenseExcelFileOptions`, `IBbvaCsvOptions`, `ISellaCsvOptions`, `ISatisPayCsvOptions`, `ITradeRepublicCsvOptions` — config per-importer (solo nomi dei conti di default; i conti controparte si risolvono da `Account.Iban`).
- `ITransferMatchingOptions` / `TransferMatchRule` (con `MatchesDescription` statico) / `TransferRuleCounterpart` / `DescriptionMatchMode` — config transfer matching.
- `DomainResultErrors.cs` — subclassi `FluentResults.Error` keyed by `DomainErrorKind`.
