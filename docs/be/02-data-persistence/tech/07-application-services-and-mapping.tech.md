# 07 - Application services and mapping (Tecnico)

## Servizi (`Application/Services/`)
| Servizio | Responsabilità |
|---|---|
| `UserService` | `GetOrCreateUserAsync(azureOid, email, name)` — JIT provisioning: valida `azureOid`/`email`, delega a `IUserRepository.GetOrCreateUserAsync`, mappa a `UserDto`. `name` non viene persistito (lo schema `Users` non ha una colonna `Name`). |
| `AccountService` | Tutte le operazioni (`GetAccounts`, `GetAccount`, `GetAvailability`, `AddAccounts`, `UpdateAccount`) richiedono/filtrano ora per `userId`. `AddAccounts`/`UpdateAccount` forzano `entity.UserId = userId` server-side dopo il mapping, ignorando qualunque `UserId` presente nel DTO in ingresso. |
| `CategoryService` | Query paginate/singole, categoria di default, add/update, delete-with-reassignment (le transazioni vengono spostate sulla categoria di default prima dell'eliminazione; bloccata l'eliminazione della categoria di default). |
| `TransactionService` | Query paginate con totali, month-options, lookup singolo/latest, add/update/delete, time-series (`GetTimeSeries` = somme per periodo, `GetStock` = totali cumulativi), `GetLanding()` (aggregazione dashboard). |
| `TransferService` | Add/update/delete; ogni trasferimento genera 2 `Transaction` collegate (leg negativa "from", leg positiva "to") con categoria `MoneyTransfers`; valida che i due conti siano diversi e l'importo positivo. |
| `ExcelImportService` | Importer Excel legacy multi-sheet (vedi [08-import-overview-and-dedup](../../03-import-pipeline/tech/08-import-overview-and-dedup.tech.md)). |
| `BbvaCsvImportService`, `SellaCsvImportService`, `SatisPayCsvImportService`, `TradeRepublicCsvImportService` | Importer CSV per banca (vedi [09-bank-specific-importers](../../03-import-pipeline/tech/09-bank-specific-importers.tech.md)). |
| `TransferMatchingService` | Motore di matching trasferimenti cross-source (vedi [10-transfer-matching](../../03-import-pipeline/tech/10-transfer-matching.tech.md)). |

## Supporto
- **`Builders/CategorySeedBuilder.cs`** — costruisce le 11 categorie seed, usate sia da `Category.CreateSeedCategories()` (Domain) sia da `CategoryConfiguration.HasData(...)` (Infrastructure).
- **`Extensions/BasicTypeExtensions.cs`** — parsing culture-aware (`ParseToDecimal`, `ParseToDateTime`), tag/keyword matching (`ContainsOne`), parsing date da nome foglio italiano (`ParseDateFromSheetName`, es. "Maggio 2026").
- **`Utilities/AzureKeyVaultHandler.cs`** — recupero secret da Key Vault via `Azure.Identity`. Non usato a runtime (wiring commentato in `Program.cs`).

## Mapping (`Mappings/ExpenseTrackerProfile.cs`)
AutoMapper profile: `Account ↔ AccountDto` con `.ForMember(dest => dest.UserId, opt => opt.Ignore())` sul reverse map (`AccountDto → Account`) — il client può leggere `UserId` nella risposta ma non impostarlo in scrittura, il valore autenticato viene sempre applicato a mano in `AccountService`; `User → UserDto` (sola andata, nessun reverse map); `Category ↔ CategoryDto` (incluso `Tags` stringa ↔ `IEnumerable<string>`), `Transaction ↔ TransactionDto`, `Transfer → TransferDto` (deriva from/to account, importo, descrizione e data dalle due leg; nessun reverse map — i trasferimenti sono costruiti manualmente in `TransferService`).

## DI
`AddApplicationServices()` (`Application/AddServicesExtensions.cs`) registra AutoMapper e tutti i servizi Application come scoped.
