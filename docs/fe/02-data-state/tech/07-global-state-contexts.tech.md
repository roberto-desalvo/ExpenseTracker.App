# 07 - Global state (stores) (Tecnico)

Lo stato condiviso è in servizi Angular `@Injectable({ providedIn: "root" })` basati su **signals** (`src/app/core/state/`). Le viste leggono i signal nei template; i caricamenti sono espliciti (metodi) o guidati da `effect()`.

## Store principali
- `PagedEntityStore<T>` (`paged-entity.store.ts`, classe astratta) — base comune di conti e categorie:
  - `all` (elenco completo, caricato una sola volta con `ensureLoaded()` dalla shell e riusato da tutte le pagine), `loaded` (true dopo il primo caricamento, anche fallito);
  - lista paginata `paged`, `isLoading`, `page`, `pageSize`, `totalCount`, `modifyPage`, `modifyPageSize`, `refreshPaged(name?)` (filtro per nome, riparte da pagina 0);
  - `activatePagedList()`: chiamato dalla vista che mostra la lista (nel costruttore, con la funzione restituita passata a `DestroyRef.onDestroy`): la lista si carica solo mentre la vista è visibile; la disattivazione azzera ricerca e pagina. Solo l'ultima richiesta paginata aggiorna lo stato (risposte fuori ordine ignorate).
- `AccountsStore` (`accounts.store.ts`) — `addAccount` (torna a pagina 0 e ricarica entrambi gli elenchi), `updateAccount`.
- `CategoriesStore` (`categories.store.ts`) — `addCategory`, `updateCategory`, `deleteCategory`, `syncTransactions(request)`.
- `TransactionsStore` (`transactions.store.ts`) — `transactions`, `isLoading`, `totalCount`, `availableMonths`, `availableMonthsLoading`, `dataVersion`, `refreshTransactions(request?)` (senza argomento ripete l'ultima richiesta; solo l'ultima risposta vince), `addTransaction`, `updateTransaction`, `deleteTransaction`, `addTransfer`. `activatePage()` è chiamato dalla pagina Movimenti: carica i mesi disponibili e segna la pagina come attiva. `dataVersion` è incrementato dopo ogni modifica: Dashboard, Analisi e Conti lo leggono in un `effect()` per ricaricare gli aggregati; mesi e lista vengono ricaricati subito solo se la pagina Movimenti è aperta.
- `TransactionsTableStore` (`transactions-table.store.ts`) — colonne (`TRANSACTION_COLUMNS`: `date`, `description`, `category`, `account`, `amount` a destra, `actions`), filtri (`selectedAccountIds`, `selectedCategoryIds`, `movementType: "all"|"incomes"|"outcomes"`, periodo: `periodMode: "month"|"custom"`, `selectedMonth`, `customRange { from; to }` in `YYYY-MM-DD` tradotto in `T00:00:00` / `T23:59:59.999`), paginazione e ordinamento (`sortBy` default `date`, `sortDirection` default `desc`; `modifySort(field)` inverte sulla stessa colonna, su una nuova parte da `desc` per data/importo e `asc` per i testi, e torna a pagina 0). `request` è un `computed` della richiesta (tutti i conti/categorie selezionati = nessun filtro); un `effect` chiama `refreshTransactions(request)` solo mentre la pagina è attiva (`activate()`) e solo quando la chiave serializzata della richiesta cambia davvero. Altri `effect` tolgono dalla selezione gli id non più esistenti e scelgono il mese più recente quando cambia l'elenco dei mesi. `modifyPeriodMode("custom")` inizializza l'intervallo dal mese selezionato se vuoto.
- `QuickActionsService` (`quick-actions.service.ts`) — apertura delle modali di inserimento montate una volta nella shell: `transactionModalOpen`, `currentTransaction` (copia della transazione in modifica, `id = 0` = nuova), `openTransactionModal(t?)`, `closeTransactionModal()`, `openNewTransaction()`, `openNewTransfer()`, `transferModalOpen`, `closeTransferModal()`.
- `NotificationService` (`core/api/notification.service.ts`) — `showError`/`showSuccess` (modulo 05).
- `ThemeService` (`core/theme/theme.service.ts`) — preferenze di aspetto e token (modulo 12).
- `AuthService` (`core/auth/auth.service.ts`) — utente corrente (modulo 04).

## Pattern usato
- Signal in lettura, metodi per le scritture; nessun provider da annidare.
- Refresh dopo CRUD dentro gli store e `dataVersion` per le viste aggregate.
- Caricamento condizionato alla vista visibile (`activatePagedList`, `activatePage`, `activate`) invece che alla rotta.
- Una chiamata per atterraggio: gli `effect` dipendono da valori stabili (chiavi stringa degli id, flag `loaded`, chiave della richiesta) e usano `untracked` per non registrare dipendenze indesiderate; le viste che usano le anagrafiche (Analisi, saldi dei Conti) aspettano il primo caricamento. Le letture identiche concorrenti sono deduplicate da `ApiClient` (modulo 05).

## Dipendenze
`TransactionsTableStore` dipende da `AccountsStore`, `CategoriesStore` e `TransactionsStore`. Le modali transazione/trasferimento usano `QuickActionsService` + `TransactionsStore`.

## Test
`transactions-table.store.spec.ts` (costruzione della richiesta, ordinamento, intervallo personalizzato, caricamento solo a pagina attiva) e `theme.service.spec.ts`.
