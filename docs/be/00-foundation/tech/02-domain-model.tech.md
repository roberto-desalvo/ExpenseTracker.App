# 02 - Domain model (Tecnico)

## Entità (`Domain/Entities/`)
- **`User`** — `Id`, `AzureOid?` (oid Azure AD dell'utente umano, nullable, indice univoco filtrato), `AppOid?` (oid dell'app/managed identity associata manualmente a questo utente, stesso pattern di indice — usato solo dal fallback JIT dell'import, vedi [04-auth-and-request-pipeline](../../01-platform/tech/04-auth-and-request-pipeline.tech.md)), `Email` (required), `IsDemo`; 1-N `Account`. Rappresenta l'utente interno, creato via JIT provisioning al primo accesso.
- **`Account`** — `Id`, `Name`, `UserId` (FK required verso `User`) + navigation `UserNavigation`; 1-N `Transaction`. Costruttore `Account(int id, string name, int userId)`.
- **`Category`** — `Id`, `Name`, `Description?`, `Priority?`, `IsDefault?`, `Tags?` (keyword list per auto-categorizzazione); 1-N `Transaction`. Seed via `Category.CreateSeedCategories()` + `Application.Builders.CategorySeedBuilder`.
- **`Transaction`** — record primario: `Amount`, `Description`, `Date?`, FK richiesta `AccountId`, FK opzionali `CategoryId`/`TransferId`, `ExternalId?` (fingerprint dedup import), `CreatedOn`/`UpdatedOn?`.
- **`Transfer`** — collega le due `Transaction` (leg) di un trasferimento; espone `TransactionFroms`/`TransactionTos` calcolati dal segno dell'importo.

Relazioni: `User` 1-N `Account` (delete Restrict), `Account` 1-N `Transaction` (delete Restrict), `Category` 1-N `Transaction` (delete SetNull), `Transfer` 1-N `Transaction` (2 leg, delete SetNull).

## Enum (`Domain/Enums/`)
- `CategoryEnum` — Default, MoneyTransfers, WorkIncomes, Housing, HealthAndFitness, FoodAndBeverage, Transportation, Entertainment, Clothes, SavingsAndInvestments, Gifts.
- `TimeGranularityEnum` — Daily, Weekly, Monthly, Yearly.
- `DomainErrorKind` — BadRequest, Validation, Unauthorized, Forbidden, NotFound, Conflict (vedi [05-error-handling](../../01-platform/tech/05-error-handling.tech.md)).

> `AccountEnum` (seed statico di 8 account: Contanti, Hype, Satispay, Trade Republic, Sella, BBVA, PayPal, Trade Republic Trading) è stato **rimosso**: gli account sono ora sempre creati per-utente in modo lazy/JIT al primo import (`EnsureAccountsExistAsync` in ciascun importer), non più seedati via `HasData`.

## DTO (`Domain/Dtos/`, `Domain/Dtos/Requests/`)
Risposta: `UserDto`, `AccountDto` (include `UserId`, non impostabile dal client — vedi [07-application-services-and-mapping](../../02-data-persistence/tech/07-application-services-and-mapping.tech.md)), `CategoryDto`, `TransactionDto`, `TransferDto`, `PagedResult<T>`, `TransactionQueryResult` (paginazione + totali income/outcome/net), `TransactionMonthOptionDto`, `TimeSeriesDto`/`TimeSeriesListDto`/`TimeSeriesPointDto`/`TimeSeriesDimensionDto`, `LandingDashboardDto` (+ Account/Category/Totals).

Richiesta: `PagedQueryRequest` (base astratta: `Page`, `PageSize`), `AccountQueryRequest`, `CategoryQueryRequest`, `TransactionQueryRequest`, `TimeSeriesRequestDto`, `ImportExcelBase64Request`.

## Interfacce repository/service (`Domain/Repositories/`, `Domain/Services/`)
- `IUserRepository`, `IAccountRepository`, `ICategoryRepository`, `ITransferRepository`, `ITransactionRepository` — implementate in Infrastructure. `IAccountRepository` espone sia overload globali (`GetAccount(id)`, `GetAccounts()`, usati da `TransactionService`/`TransferService`, fuori scope per lo user-scoping) sia overload filtrati per utente (`GetAccount(id, userId)`, `GetAccounts(userId)`, `GetPagedAccounts(request, userId)`, usati da `AccountService`/`AccountController` e dalla pipeline di import).
- `IUserService`, `IAccountService`, `ICategoryService`, `ITransactionService`, `ITransferService` — implementate in Application, ritornano `FluentResults.Result`/`Result<T>`.
- `ICurrentUserAccessor` — `GetUserIdAsync()` (usato da `AccountController`/`AuthController`, richiede sempre email) e `GetUserIdForImportAsync()` (usato solo da `ImportController`, tollera l'assenza di email e fa fallback sull'`AppOid`), vedi [04-auth-and-request-pipeline](../../01-platform/tech/04-auth-and-request-pipeline.tech.md).
- `IExcelImportService`, `IBbvaCsvImportService`, `ISellaCsvImportService`, `ISatisPayCsvImportService`, `ITradeRepublicCsvImportService`, `ITransferMatchingService` — vedi modulo [08-import-overview-and-dedup](../../03-import-pipeline/tech/08-import-overview-and-dedup.tech.md).

## Contratti cross-cutting (`Domain/Common/`)
- `IAppOptions`, `IService`, `IRepository` — marker interfaces per DI reflection-based.
- `IExpenseExcelFileOptions`, `IBbvaCsvOptions`, `ISellaCsvOptions`, `ISatisPayCsvOptions`, `ITradeRepublicCsvOptions` — config per-importer.
- `ITransferMatchingOptions` / `TransferMatchRule` / `DescriptionMatchMode` — config transfer matching.
- `DomainResultErrors.cs` — subclassi `FluentResults.Error` keyed by `DomainErrorKind`.
