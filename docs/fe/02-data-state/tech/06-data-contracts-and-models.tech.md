# 06 - Data contracts and models (Tecnico)

## Modelli principali (`src/app/core/models/`)
- `Account` (interface): `{ id: number; name: string; iban?: string | null }`.
- `Category` (interface): `{ id: number; priority: number; name: string; description: string; isDefault?: boolean; tags: string[] }`.
- `Transaction` (**classe** con costruttore posizionale): `id: number`, `date: Date`, `description: string`, `amount: number`, `categoryId: number`, `category: string`, `accountId: number`, `account: string`. Serializzata con `JSON.stringify` (la data diventa ISO).
- `TransferPayload` (`Transfer.ts`): `{ id?: number; fromAccountId: number; toAccountId: number; amount: number; description: string; date?: Date | null }`.
- `LandingDashboard`: `{ asOf: string; monthStart: string; accounts: LandingAccountBalance[]; categories: LandingCategorySummary[]; totals: LandingTotals; netWorthSeries: TimeSeriesList }`.
  - `LandingAccountBalance`: `{ accountId; name; currentBalance; spentMonth; earnedMonth; netMonth }`.
  - `LandingCategorySummary`: `{ categoryId; name; spentMonth; earnedMonth; netMonth }`.
  - `LandingTotals`: `{ currentBalanceTotal; spentMonth; earnedMonth; netMonth }`.
- `TimeSeries*` (`TimeSeries.ts`): `TimeSeriesDimension { key; value }`, `TimeSeriesPoint { period; amount; earned; spent }`, `TimeSeries { dimensions; values }`, `TimeSeriesList { granularity; series: TimeSeries[] }`.
- `TimeSeriesRequest`: `{ startDate; endDate; idAccounts: number[]; idCategories: number[]; granularity: number; excludeTransfers?: boolean }`.
- `TransactionQueryRequest`: `{ fromDate?; toDate?; idAccounts?: number[] | null; idCategories?: number[] | null; isIncome?: boolean | null; sortBy?: TransactionSortField; sortDirection?: SortDirection; page: number; pageSize: number }`, con `TransactionSortField = "date" | "description" | "category" | "account" | "amount"` e `SortDirection = "asc" | "desc"`. La risposta di `POST transaction/query` è un `PagedResult<Transaction>` (nessun totale).
- `PagedResult<T>`: `{ items: T[]; totalCount: number; page: number; pageSize: number }`, riusato da tutte le liste paginate.
- `CategorySyncRequest` / `CategorySyncResult` (esportati da `src/app/core/api/category.api.ts`): `{ startDate: string; endDate: string }` (date `yyyy-MM-dd`, estremi inclusi) e `{ examinedTransactions; updatedTransactions; unmatchedTransactions }`, payload di `POST api/category/sync`.
- `TransactionMonthOption`: `{ startDate: string; endDate: string; description: string }`, per il selettore mese della tabella movimenti.

## Ruolo
Tipizzare payload request/response tra servizi API e UI. I modelli sono stati portati invariati dal frontend React.

## Sorgenti
- `src/app/core/models/*`
- `src/app/core/api/account.api.ts`, `category.api.ts`, `transaction.api.ts`
