# 06 - Database and EF Core (Tecnico)

## `ExpenseTrackerContext`
`DbSet<Transaction>`, `DbSet<Account>`, `DbSet<Category>`, `DbSet<Transfer>`. `OnModelCreating` applica tutte le `IEntityTypeConfiguration<T>` dell'assembly. Provider SQL Server con `EnableRetryOnFailure()` e timeout comandi 60s (`Infrastructure/AddServicesExtensions.cs`).

## Entity configurations (`Infrastructure/EntityConfigurations/`)
- **`TransactionConfiguration`** — `Amount` `decimal(18,2)`; `ExternalId` con **indice univoco filtrato** (`WHERE ExternalId IS NOT NULL`) — meccanismo cardine di dedup import.
- **`TransferConfiguration`** — stesso indice univoco filtrato su `ExternalId`.
- **`AccountConfiguration`** — seed di tutti i valori `AccountEnum` via `HasData`.
- **`CategoryConfiguration`** — seed di tutti i valori `CategoryEnum` via `CategorySeedBuilder`.

## Repository (`Infrastructure/Repositories/`)
`RepositoryBase` (condivide `SaveChangesAsync`) estesa da `AccountRepository`, `CategoryRepository`, `TransferRepository`, `TransactionRepository`. Note:
- `AccountRepository.GetAvailability` calcola il saldo sommando le transazioni (nessun saldo memorizzato).
- `CategoryRepository.ReassignTransactionsToCategory` usa `ExecuteUpdateAsync` (bulk update SQL).
- `TransactionRepository` implementa le query di aggregazione della dashboard (`GetAccountBalances`, `GetCategoryMonthTotals`, `GetMonthTotals`), le time-series, e `GetUnlinkedTransferCandidates` usata dal transfer matching.

## Migrations (`Infrastructure/Migrations/`)
1. `Initialize_Database` — schema iniziale.
2. `Add_Trade_Republic_Ingestion` — aggiunge `Transactions.ExternalId` (indice univoco filtrato) e seed account "Trade Republic Trading".
3. `AddExternalIdToTransfer` — aggiunge `Transfers.ExternalId` (indice univoco filtrato).

`Migrations/Scripts/` contiene uno script SQL idempotente generato per la migration iniziale (deploy manuale/DBA). `Migrations/readme.md` documenta il workflow CLI `dotnet ef`.

## Comandi
```powershell
dotnet ef migrations add <MigrationName> -p api/RDS.ExpenseTracker.Infrastructure -s api/RDS.ExpenseTracker.Api -o Migrations
dotnet ef database update -p api/RDS.ExpenseTracker.Infrastructure -s api/RDS.ExpenseTracker.Api
```

## DI
`AddInfrastructureServices(connectionString)` registra `ExpenseTrackerContext` e tutti i repository come scoped.
