# 07 - Global state contexts (Tecnico)

## Context principali
- `AccountContext.tsx` → provider `AccountsProvider`, hook `useAccounts()`. Espone `accounts` (elenco completo), `pagedAccounts`, `isLoading`, `page`, `pageSize`, `totalCount`, `modifyPage`, `modifyPageSize`, `refreshAccounts(name?)`, `addAccount`, `updateAccount`. Carica i dati quando la rotta corrente è in `DATA_ROUTES` (`src/config/routes.ts`).
- `CategoryContext.tsx` → provider `CategoriesProvider`, hook `useCategories()`. Espone `categories` (pagina corrente), `allCategories` (elenco completo, usato da filtri, modale transazione e analisi), `isLoading`, `page`, `pageSize`, `totalCount`, `modifyPage`, `modifyPageSize`, `addCategory`, `updateCategory`, `deleteCategory`, `refreshCategories(name?)`. Stesso vincolo `DATA_ROUTES`.
- `TransactionContext.tsx` → provider `TransactionsProvider`, hook `useTransactions()`. Espone `transactions`, `isLoading`, `totalCount`, `totalIncomes`, `totalOutcomes`, `totalNet`, `availableMonths`, `availableMonthsLoading`, `dataVersion`, `addTransaction`, `addTransfer`, `updateTransaction`, `deleteTransaction`, `refreshTransactions(request?)`. Carica i mesi disponibili solo sulla rotta `/movimenti`. `dataVersion` è un contatore incrementato dopo ogni add/update/delete/transfer: Dashboard, Analisi e Conti lo mettono tra le dipendenze dei propri `useEffect` per ricaricare gli aggregati.
- `TableContext.tsx` → provider `TableContextProvider`, hook `useTableContext()`. Definisce le colonne della tabella movimenti (`date`, `description`, `category`, `account`, `amount` allineato a destra, `actions`), i filtri (`selectedAccountIds`, `selectedCategoryIds`, `selectedMonth`, `movementType: "all"|"incomes"|"outcomes"`), la paginazione e orchestra `refreshTransactions`. La normalizzazione "tutte le categorie selezionate = nessun filtro" usa `allCategories`. Carica dati solo sulla rotta `/movimenti`.
- `TransactionModalContext.tsx` → provider `TransactionModalProvider`, hook `useTransactionModal()`: stato del form di creazione/modifica (`transactionModalOpen`, `currentTransaction`, `openTransactionModal(transaction?)`, `closeTransactionModal`, `modify*`, `sendTransaction`).
- `QuickActionsContext.tsx` → provider `QuickActionsProvider`, hook `useQuickActions()`: `openNewTransaction()`, `openNewTransfer()`. Monta una sola istanza di `TransactionModal` e `TransferModal` per tutta l'app.
- `ApiErrorContext.tsx` → `useApiError()`, `showError(message)`; ascolta `API_ERROR_EVENT` di `ApiClient.ts`.
- `SuccessMessageContext.tsx` → `useSuccessMessage()`, `showSuccess(message)`; usato da pagine e righe tabella per le conferme (sopravvive allo smontaggio del componente che lo invoca).
- `ThemeContext.tsx` → provider `AppThemeProvider`, hook `useAppTheme()`: `mode` effettivo, `preferences` (`preset`, `mode` incl. `system`, `nav`), `toggleTheme`, `setPreset`, `setModePreference`, `setNavLayout` (dettaglio nel modulo 12).

## Pattern usato
- `Provider + custom hook`; ogni hook lancia un `Error` se invocato fuori dal proprio Provider.
- Refresh dopo CRUD (le funzioni `add*`/`update*`/`delete*` richiamano il refresh corrispondente) e `dataVersion` per le viste aggregate che non passano dal context.
- Caricamento condizionato alla rotta tramite `useLocation`.

## Nesting dei provider
- `src/main.tsx`: `MsalProvider > AppThemeProvider > ApiErrorProvider > SuccessMessageProvider > ...`.
- `src/App.tsx`: `AccountsProvider > TransactionsProvider > CategoriesProvider > TableContextProvider > TransactionModalProvider > QuickActionsProvider > AppLayout`.

## Dipendenze
`TableContext` dipende da `useAccounts`, `useCategories` e `useTransactions`. `TransactionModalContext` dipende da `useTransactions`. `QuickActionsContext` dipende da `useTransactionModal`.
