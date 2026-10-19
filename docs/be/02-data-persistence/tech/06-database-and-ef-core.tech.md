# 06 - Database and EF Core (Tecnico)

## `ExpenseTrackerContext`
`DbSet<Transaction>`, `DbSet<Account>`, `DbSet<Category>`, `DbSet<Transfer>`, `DbSet<User>`. `OnModelCreating` applica tutte le `IEntityTypeConfiguration<T>` dell'assembly. Provider SQL Server con `EnableRetryOnFailure()` e timeout comandi 60s (`Infrastructure/AddServicesExtensions.cs`).

## Entity configurations (`Infrastructure/EntityConfigurations/`)
- **`TransactionConfiguration`** — `Amount` `decimal(18,2)`; **indice univoco filtrato** su `(AccountId, ExternalId)` (`WHERE ExternalId IS NOT NULL`) — backstop DB della dedup import, per conto e quindi per utente (lo stesso movimento può essere importato da utenti diversi). Indice `AccountId` dichiarato esplicitamente per la FK: quello composito è filtrato e non copre i movimenti senza `ExternalId`.
- **`TransferConfiguration`** — indice filtrato **non univoco** su `ExternalId`: un `Transfer` non ha conto né utente propri, quindi l'unicità per utente è solo applicativa (`GetTransferByExternalId(externalId, userId)`).
- **`AccountConfiguration`** — chiave primaria, `Name` required (max 100), relazione 1-N con `Transaction` (delete Restrict). **Non seeda più account**: l'`HasData` con gli 8 account fissi (Contanti, Hype, Satispay, Trade Republic, Sella, BBVA, PayPal, Trade Republic Trading) è stato rimosso insieme ad `AccountEnum` — gli account sono ora creati lazy per-utente dalla pipeline di import.
- **`CategoryConfiguration`** — seed di tutti i valori `CategoryEnum` via `CategorySeedBuilder`.

## Tabella `Users`
Nuova entità (`Domain/Entities/User.cs`): `Id` (identity), `AzureOid` (`nvarchar(64)`, nullable) con **indice univoco filtrato** `IX_Users_AzureOid` (`WHERE [AzureOid] IS NOT NULL`) per evitare duplicati sullo stesso oid Azure AD mantenendo l'utente creabile prima che l'oid sia noto, `AppOid` (`nvarchar(64)`, nullable) con lo stesso pattern di indice univoco filtrato `IX_Users_AppOid` — object id di un'app/managed identity associata **manualmente via SQL** a questo utente, usato solo dal fallback JIT dell'import (vedi [04-auth-and-request-pipeline](../../01-platform/tech/04-auth-and-request-pipeline.tech.md)), `Email` (`nvarchar(256)`, required), `IsDemo` (`bit`, default `false`). `Accounts.UserId` (colonna `int` **required**, FK `FK_Accounts_Users_UserId` con `onDelete: Restrict`, indice `IX_Accounts_UserId`).

## Repository (`Infrastructure/Repositories/`)
`RepositoryBase` (condivide `SaveChangesAsync`) estesa da `AccountRepository`, `CategoryRepository`, `TransferRepository`, `TransactionRepository`, `UserRepository`. Note:
- `AccountRepository.GetAvailability` calcola il saldo sommando le transazioni (nessun saldo memorizzato); ora richiede `userId` e verifica che l'account appartenga all'utente prima di sommare.
- `AccountRepository` espone solo letture filtrate per utente (`GetAccount(id, userId)`, `GetAccounts(userId)`, `GetPagedAccounts(request, userId)`).
- `TransactionRepository` filtra per utente tramite il conto: un helper privato `ForUser(userId)` = `Context.Transactions.Where(t => t.AccountNavigation.UserId == userId)` è la base di tutte le letture che ricevono `userId` (`GetTransaction`, `GetTransactions`, `GetPagedTransactions`, time-series, saldi e totali della landing, `GetAvailableMonthRanges`, `GetLatestTransaction`, `DeleteAllTransactions`/`ResetTransactions`, `GetExistingExternalIds` per la dedup import). Restano senza filtro utente solo i metodi interni alla pipeline di import/transfer già vincolati a conti dell'utente (`GetUnlinkedTransferCandidates(accountId, …)`, `GetTransactionsByTransferId`, `DeleteTransactionsByAccountIds`).
- `TransferRepository.GetTransfer(id, userId)`/`GetTransfers(userId)`/`GetTransferByExternalId(externalId, userId)` restituiscono solo i trasferimenti con almeno un movimento su un conto dell'utente (`Transactions.Any(t => t.AccountNavigation.UserId == userId)`).
- `CategoryRepository.ReassignTransactionsToCategory` usa `ExecuteUpdateAsync` (bulk update SQL).
- `TransactionRepository` implementa le query di aggregazione della dashboard (`GetAccountBalances`, `GetCategoryMonthTotals`, `GetMonthTotals`), le time-series, e `GetUnlinkedTransferCandidates` usata dal transfer matching.
- `UserRepository.GetOrCreateUserAsync(azureOid, email)` — cerca per `AzureOid` (`GetByAzureOid`), altrimenti crea l'utente; in caso di `DbUpdateException` per violazione dell'indice univoco filtrato (`SqlException` 2601/2627, race tra richieste concorrenti sullo stesso primo login) fa detach dell'entità e ri-legge l'utente creato dalla richiesta vincitrice. `GetByAppOid(appOid)` — lookup aggiuntivo usato solo dal fallback JIT dell'import (`UserService.GetOrCreateUserForImportAsync`). `GetByEmail(email)` — usato solo dal login di sviluppo (`UserService.GetUserByEmailAsync`); l'email non è univoca, quindi ordina per `AppOid != null` decrescente e poi per `Id`.

## Migrations (`Infrastructure/Migrations/`)
1. `Initialize_Database` — schema iniziale.
2. `Add_Trade_Republic_Ingestion` — aggiunge `Transactions.ExternalId` (indice univoco filtrato) e seed account "Trade Republic Trading".
3. `AddExternalIdToTransfer` — aggiunge `Transfers.ExternalId` (indice univoco filtrato).
4. `Add_Users_And_Account_UserId_Nullable` — prima fase dell'introduzione degli utenti, per preservare i dati esistenti: crea la tabella `Users`, aggiunge `Accounts.UserId` come colonna **nullable** (non `NOT NULL`) con FK verso `Users` (Restrict). Gli 8 account seed preesistenti restano con `UserId = NULL` (l'`HasData`/`DeleteData` scaffoldato da EF per quei seed è stato rimosso a mano, perché quelle righe hanno già `Transaction` reali collegate via FK Restrict) fino a un backfill manuale via SQL dopo il primo login reale.
5. `Make_Account_UserId_Required` — seconda fase, applicata dopo il backfill manuale: `ALTER COLUMN Accounts.UserId int NOT NULL` (`AlterColumn` con `oldNullable: true`). L'entità `Account.UserId` è passata da `int?` a `int` non nullable in `Domain/Entities/Account.cs`, e la FK in `UserConfiguration` da `.IsRequired(false)` a `.IsRequired()`.
6. `Add_AppOid_To_Users` — puramente additiva: aggiunge `Users.AppOid` (`nvarchar(64)`, nullable) con indice univoco filtrato `IX_Users_AppOid`, per il fallback JIT dell'import (app/managed identity). Nessun impatto sui dati esistenti.
7. `Scope_ExternalId_Per_User` — dedup import per utente: sostituisce l'indice univoco `IX_Transactions_ExternalId` con l'indice univoco filtrato `IX_Transactions_AccountId_ExternalId` su `(AccountId, ExternalId)` e ricrea `IX_Transfers_ExternalId` come **non univoco**. Solo indici, nessuna modifica ai dati; l'`Up` non può fallire sui dati esistenti (ogni `ExternalId` era già unico). Il `Down` ripristina gli indici univoci globali e fallisce se nel frattempo due utenti hanno importato lo stesso `ExternalId`.

`Migrations/Scripts/` contiene uno script SQL idempotente generato per la migration iniziale (deploy manuale/DBA). `Migrations/readme.md` documenta il workflow CLI `dotnet ef`.

## Comandi
```powershell
dotnet ef migrations add <MigrationName> -p api/RDS.ExpenseTracker.Infrastructure -s api/RDS.ExpenseTracker.Functions -o Migrations
dotnet ef database update -p api/RDS.ExpenseTracker.Infrastructure -s api/RDS.ExpenseTracker.Functions
```

## DI
`AddInfrastructureServices(connectionString)` registra `ExpenseTrackerContext` e tutti i repository come scoped.
