# 02 - Domain model (Tecnico)

## Entità (`Domain/Entities/`)
- **`Account`** — `Id`, `Name`; 1-N `Transaction`.
- **`Category`** — `Id`, `Name`, `Description?`, `Priority?`, `IsDefault?`, `Tags?` (keyword list per auto-categorizzazione); 1-N `Transaction`. Seed via `Category.CreateSeedCategories()` + `Application.Builders.CategorySeedBuilder`.
- **`Transaction`** — record primario: `Amount`, `Description`, `Date?`, FK richiesta `AccountId`, FK opzionali `CategoryId`/`TransferId`, `ExternalId?` (fingerprint dedup import), `CreatedOn`/`UpdatedOn?`.
- **`Transfer`** — collega le due `Transaction` (leg) di un trasferimento; espone `TransactionFroms`/`TransactionTos` calcolati dal segno dell'importo.

Relazioni: `Account` 1-N `Transaction` (delete Restrict), `Category` 1-N `Transaction` (delete SetNull), `Transfer` 1-N `Transaction` (2 leg, delete SetNull).

## Enum (`Domain/Enums/`)
- `CategoryEnum` — Default, MoneyTransfers, WorkIncomes, Housing, HealthAndFitness, FoodAndBeverage, Transportation, Entertainment, Clothes, SavingsAndInvestments, Gifts.
- `AccountEnum` — Contanti, Hype, Satispay, TradeRepublic, Sella, BBVA, PayPal, TradeRepublicTrading (seed iniziale account, nome da `[Description]`).
- `TimeGranularityEnum` — Daily, Weekly, Monthly, Yearly.
- `DomainErrorKind` — BadRequest, Validation, Unauthorized, Forbidden, NotFound, Conflict (vedi [05-error-handling](../../01-platform/tech/05-error-handling.tech.md)).

## DTO (`Domain/Dtos/`, `Domain/Dtos/Requests/`)
Risposta: `AccountDto`, `CategoryDto`, `TransactionDto`, `TransferDto`, `PagedResult<T>`, `TransactionQueryResult` (paginazione + totali income/outcome/net), `TransactionMonthOptionDto`, `TimeSeriesDto`/`TimeSeriesListDto`/`TimeSeriesPointDto`/`TimeSeriesDimensionDto`, `LandingDashboardDto` (+ Account/Category/Totals).

Richiesta: `PagedQueryRequest` (base astratta: `Page`, `PageSize`), `AccountQueryRequest`, `CategoryQueryRequest`, `TransactionQueryRequest`, `TimeSeriesRequestDto`, `ImportExcelBase64Request`.

## Interfacce repository/service (`Domain/Repositories/`, `Domain/Services/`)
- `IAccountRepository`, `ICategoryRepository`, `ITransferRepository`, `ITransactionRepository` — implementate in Infrastructure.
- `IAccountService`, `ICategoryService`, `ITransactionService`, `ITransferService` — implementate in Application, ritornano `FluentResults.Result`/`Result<T>`.
- `IExcelImportService`, `IBbvaCsvImportService`, `ISellaCsvImportService`, `ISatisPayCsvImportService`, `ITradeRepublicCsvImportService`, `ITransferMatchingService` — vedi modulo [08-import-overview-and-dedup](../../03-import-pipeline/tech/08-import-overview-and-dedup.tech.md).

## Contratti cross-cutting (`Domain/Common/`)
- `IAppOptions`, `IService`, `IRepository` — marker interfaces per DI reflection-based.
- `IExpenseExcelFileOptions`, `IBbvaCsvOptions`, `ISellaCsvOptions`, `ISatisPayCsvOptions`, `ITradeRepublicCsvOptions` — config per-importer.
- `ITransferMatchingOptions` / `TransferMatchRule` / `DescriptionMatchMode` — config transfer matching.
- `DomainResultErrors.cs` — subclassi `FluentResults.Error` keyed by `DomainErrorKind`.
