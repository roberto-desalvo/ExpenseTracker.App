# 10 - Accounts and transfers flow (Tecnico)

## Accounts
- Pagina: `src/pages/AccountsPage.tsx`, gestisce solo il CRUD dei conti (creazione/modifica del nome). Non c'è azione di eliminazione: `RowActionsMenu` espone unicamente "Modifica"; coerentemente `AccountService` non ha un metodo `delete`.
- La pagina accetta una prop `embedded?: boolean` (default `false`), con lo stesso comportamento di `CategoriesPage`: quando `embedded` è `true` viene soppresso il titolo `Typography` "Account" e il padding esterno è azzerato. Non esiste più una route standalone `/account`: `AccountsPage` viene montata solo da `src/pages/SettingsPage.tsx` come tab "Account" della route `/impostazioni` (`?tab=account`), invocata con `<AccountsPage embedded />` (vedi modulo `03-routing-and-shell`).
- Stato: `AccountContext` (`pagedAccounts` per la tabella, `accounts` per le liste complete usate altrove, `page`, `pageSize`, `totalCount`, `addAccount`, `updateAccount`, `refreshAccounts`).
- API: `AccountService` (`query`, `add`, `update`).
- Filtro: `AccountsFilterBar`, stessa logica di debounce (350ms, ricerca da 3 caratteri) di `CategoriesFilterBar`.
- Colonne tabella: `Nome`, `Azioni`.

## Transfers
- Non c'è più un'azione trasferimenti dentro `AccountsPage`: i trasferimenti si creano dalla home, tramite `AccountsBar` (mostrata sopra `ExpenseTable` in `LandingPage`), menu "Aggiungi" → voce "Aggiungi trasferimento" → apre `TransferModal`.
- `TransferModal` raccoglie un `TransferPayload` (`fromAccountId`, `toAccountId`, `amount`, `description`, `date`) e invoca `TransactionContext.addTransfer()`, che a sua volta chiama `TransferService.add()`.
- Dopo l'aggiunta di un trasferimento, il contesto ricarica i mesi disponibili e rilancia il refresh delle transazioni filtrate (i due movimenti generati dal trasferimento compaiono quindi nell'elenco transazioni).

## Analisi account
`AccountsPage` non integra più alcun grafico: non importa `TransactionService` né `TimeSeriesLineChart`. Il grafico di andamento per account (basato su `TransactionService.getStock()`) è ora nella sezione "Patrimonio" di `src/pages/LandingPage.tsx` (vedi modulo `11-dashboard-and-time-series` per il dettaglio dei grafici e dei filtri data/granularità).
