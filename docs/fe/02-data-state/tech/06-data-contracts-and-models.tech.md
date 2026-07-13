# 06 - Data contracts and models (Tecnico)

## Modelli principali
- `Account` (`src/models/Account.tsx`, interface): `{ id: number; name: string }`.
- `Category` (`src/models/Category.tsx`, interface): `{ id: number; priority: number; name: string; description: string; isDefault?: boolean; tags: string[] }`.
- `Transaction` (`src/models/Transaction.tsx`, **classe** non interface, con costruttore posizionale): campi `id: number`, `date: Date`, `description: string`, `amount: number`, `categoryId: number`, `category: string`, `accountId: number`, `account: string`.
- `TransferPayload` (`src/models/Transfer.ts`): `{ id?: number; fromAccountId: number; toAccountId: number; amount: number; description: string; date?: Date | null }`.
- `LandingDashboard` (`src/models/LandingDashboard.ts`): `{ asOf: string; monthStart: string; accounts: LandingAccountBalance[]; categories: LandingCategorySummary[]; totals: LandingTotals; netWorthSeries: TimeSeriesList }`.
  - `LandingAccountBalance`: `{ accountId; name; currentBalance; spentMonth; earnedMonth; netMonth }`.
  - `LandingCategorySummary`: `{ categoryId; name; spentMonth; earnedMonth; netMonth }`.
  - `LandingTotals`: `{ currentBalanceTotal; spentMonth; earnedMonth; netMonth }`.
- `TimeSeries*` (`src/models/TimeSeries.ts`): `TimeSeriesDimension { key; value }`, `TimeSeriesPoint { period; amount }`, `TimeSeries { dimensions; values }`, `TimeSeriesList { granularity; series: TimeSeries[] }`.
- `TimeSeriesRequest` (`src/models/TimeSeriesRequest.ts`): `{ startDate; endDate; idAccounts: number[]; idCategories: number[]; granularity: number; excludeTransfers?: boolean }`.
- `TransactionQuery*`:
  - `TransactionQueryRequest` (`src/models/TransactionQueryRequest.ts`): `{ fromDate?; toDate?; idAccounts?: number[] | null; idCategories?: number[] | null; isIncome?: boolean | null; page: number; pageSize: number }`.
  - `TransactionQueryResult` (`src/models/TransactionQueryResult.ts`): estende `PagedResult<Transaction>` aggiungendo `totalIncomes`, `totalOutcomes`, `totalNet`.
- `PagedResult<T>` (`src/models/PagedResult.ts`): `{ items: T[]; totalCount: number; page: number; pageSize: number }`, generico riusato da tutte le liste paginate (account, categorie, transazioni).
- `TransactionMonthOption` (`src/models/TransactionMonthOption.ts`): `{ startDate: string; endDate: string; description: string }`, usato per popolare il selettore mese della tabella movimenti.

## Ruolo
Tipizzare payload request/response tra servizi e UI.

## Sorgenti
- `src/models/*`
- `src/services/AccountService.tsx`
- `src/services/CategoryService.tsx`
- `src/services/TransactionService.tsx`
- `src/services/TransferService.ts`
