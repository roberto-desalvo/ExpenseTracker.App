# 07 - Global state contexts (Tecnico)

## Context principali
- `AccountContext.tsx` → provider `AccountsProvider`, hook `useAccounts()`. Espone `accounts`, `pagedAccounts`, `isLoading`, `page`, `pageSize`, `totalCount`, `modifyPage`, `modifyPageSize`, `refreshAccounts(name?)`, `addAccount`, `updateAccount`. Carica i dati solo quando la rotta corrente è `/` o `/impostazioni` (controllo via `useLocation`).
- `CategoryContext.tsx` → provider `CategoriesProvider`, hook `useCategories()`. Espone `categories`, `allCategories`, `isLoading`, `page`, `pageSize`, `totalCount`, `modifyPage`, `modifyPageSize`, `addCategory`, `updateCategory`, `deleteCategory`, `refreshCategories(name?)`. Stesso vincolo di rotta (`/` o `/impostazioni`).
- `TransactionContext.tsx` → provider `TransactionsProvider`, hook `useTransactions()`. Espone `transactions`, `isLoading`, `totalCount`, `totalIncomes`, `totalOutcomes`, `totalNet`, `availableMonths`, `availableMonthsLoading`, `addTransaction`, `addTransfer`, `updateTransaction`, `deleteTransaction`, `refreshTransactions(request?)`. Carica i mesi disponibili solo sulla rotta `/`.
- `TableContext.tsx` → provider `TableContextProvider`, hook `useTableContext()`. Non è un semplice contenitore dati: definisce le colonne della tabella movimenti (`columns: TableColumn[]`, con id `date|description|amount|category|account|actions`), i filtri correnti (`selectedAccountIds`, `selectedCategoryIds`, `selectedMonth`, `movementType: "all"|"incomes"|"outcomes"`), la paginazione locale (`page`, `pageSize`) e orchestra la chiamata a `refreshTransactions` di `TransactionContext` ogni volta che filtri/mese/pagina cambiano. Espone anche `getFilteredTransactions()` (ordina le transazioni correnti per data decrescente) e `availableMonths`/`availableMonthsLoading` derivati da `TransactionContext`. Carica dati solo sulla rotta `/`.
- `TransactionModalContext.tsx` → provider `TransactionModalProvider`, hook `useTransactionModal()`. Gestisce lo stato del form modale di creazione/modifica movimento: `transactionModalOpen`, `currentTransaction`, `openTransactionModal(transaction?)`, `closeTransactionModal`, `modifyCategory`, `modifyDescription`, `modifyAmount`, `modifyAccount`, `modifyDate`, `sendTransaction`. `sendTransaction` chiama `updateTransaction` se `currentTransaction.id > 0`, altrimenti `addTransaction`, delegando a `TransactionContext`.
- `ApiErrorContext.tsx` → provider `ApiErrorProvider`, hook `useApiError()`. Espone `showError(message: string)`; si iscrive anche all'evento globale `API_ERROR_EVENT` emesso da `src/services/ApiClient.ts` e mostra una Snackbar MUI di errore.
- `SuccessMessageContext.tsx` → provider `SuccessMessageProvider`, hook `useSuccessMessage()`. Espone `showSuccess(message: string)` e mostra una Snackbar MUI di successo.
- `ThemeContext.tsx` → provider `AppThemeProvider`, hook `useAppTheme()`. Espone `mode: "light" | "dark"` e `toggleTheme()`; applica anche `MuiThemeProvider` (tema `lightTheme`/`darkTheme`) e `CssBaseline`.

## Pattern usato
- `Provider + custom hook` per accesso stato/azioni; ogni hook lancia un `Error` se invocato fuori dal proprio Provider.
- Refresh dati dopo operazioni CRUD (le funzioni `add*`/`update*`/`delete*` richiamano il refresh corrispondente al termine).
- Alcuni provider caricano dati solo su rotte specifiche tramite `useLocation`: `AccountsProvider` e `CategoriesProvider` su `/` e `/impostazioni`; `TransactionsProvider` e `TableContextProvider` solo su `/`.

## Nesting dei provider
- `src/main.tsx`: `MsalProvider > AppThemeProvider > ApiErrorProvider > SuccessMessageProvider > ...`.
- `src/App.tsx`: `AccountsProvider > TransactionsProvider > CategoriesProvider > TableContextProvider > TransactionModalProvider`.

## Dipendenze
`TableContext` dipende da `useAccounts`, `useCategories` e `useTransactions` per costruire filtri/query e per richiamare il refresh delle transazioni. `TransactionModalContext` dipende da `useTransactions` per inviare le modifiche (`addTransaction`/`updateTransaction`).
