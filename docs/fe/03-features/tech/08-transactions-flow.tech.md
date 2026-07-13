# 08 - Transactions flow (Tecnico)

## Schermata
Non esiste più una pagina dedicata `TransactionsPage.tsx`: la gestione delle transazioni è integrata in `src/pages/LandingPage.tsx` (route `/`, vedi modulo `03-routing-and-shell` per il routing), nella sezione comprimibile "Questo mese". Non ci sono tab Gestione/Analisi per le transazioni: la pagina è organizzata in due sezioni espandibili/comprimibili ("Questo mese" e "Patrimonio"; per il dettaglio dei grafici della sezione "Patrimonio" vedi modulo `11-dashboard-and-time-series`).

## Componenti chiave
- `TransactionsSummaryBar` — prop `showChips` (default `true`); in `LandingPage` viene invocato con `showChips={false}` e quindi non renderizza nulla (i chip Entrate/Uscite/Bilancio sono nascosti in home).
- `AccountsBar` — barra filtri/azioni: multi-select account, multi-select categorie, select mese (da `TableContext.availableMonths`), pulsante "Aggiungi" (menu con "Aggiungi transazione" → `TransactionModalContext.openTransactionModal()` e "Aggiungi trasferimento" → apre `TransferModal`), pulsante "Aggiorna" (`TransactionContext.refreshTransactions()`).
- `ExpenseTable` — wrapper su `DataTableBase` che usa le colonne di `TableContext` e renderizza righe con `ExpenseTableRow`.
- `ExpenseTableRow` — singola riga: azioni "Modifica"/"Elimina" via `RowActionsMenu`; la conferma di eliminazione è un `Dialog` MUI inline nel componente stesso (non usa il componente condiviso `ConfirmDeleteDialog`).
- `TransactionModal` — form add/edit transazione, montato una sola volta in fondo a `LandingPage` e pilotato da `TransactionModalContext`.
- `TransferModal` — form nuovo trasferimento, montato dentro `AccountsBar` e aperto/chiuso con stato locale.
- `AccountBoxItem.tsx` esiste ancora nel codice ma non è più referenziato da nessun altro componente (component non usato/orfano).

## Logica
- Query orchestrate da `TableContext` (filtri: account, categorie, mese, tipo movimento entrate/uscite/tutti, paginazione) + `TransactionContext` (fetch, totali, mesi disponibili). Il caricamento dati è gated da `location.pathname === "/"`.
- CRUD transazioni via `TransactionModalContext` (stato form + `sendTransaction()`) che chiama `TransactionContext.addTransaction`/`updateTransaction`, a loro volta su `TransactionService.add`/`update`/`delete`.
- Eliminazione: `ExpenseTableRow.onConfirmDelete` → `TransactionContext.deleteTransaction(transaction)` → `TransactionService.delete(id)`.
- Trasferimenti: `TransferModal` raccoglie `TransferPayload` (`fromAccountId`, `toAccountId`, `amount`, `description`, `date`) e chiama `TransactionContext.addTransfer()` → `TransferService.add()`.
- Dopo ogni add/update/delete/transfer, il contesto ricarica anche i mesi disponibili (`loadAvailableMonths`) e rilancia `refreshTransactions()`.
