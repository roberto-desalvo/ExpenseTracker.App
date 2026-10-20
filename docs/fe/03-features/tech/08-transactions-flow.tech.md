# 08 - Transactions flow (Tecnico)

## Schermata
`src/pages/TransactionsPage.tsx` (route `/movimenti`): `PageHeader` con `NewEntryButton`, poi `TransactionFilters`, `TransactionsSummaryBar` ed `ExpenseTable`. La dashboard mostra solo gli ultimi movimenti in sola lettura (modulo 11).

## Componenti chiave
- `TransactionFilters` (`src/components/TransactionFilters.tsx`) — selettore mese con frecce precedente/successivo (i mesi di `TableContext.availableMonths` arrivano dal più recente) e `Select` dei mesi; multi-select conti (`accounts`) e categorie (`allCategories`) con voce "Tutti" che azzera il filtro (array vuoto); bottone "Aggiorna". Sotto, chip rimovibili per ogni filtro attivo (conti, categorie, solo entrate/uscite) e "Azzera filtri".
- `TransactionsSummaryBar` — tre tile: Entrate e Uscite sono anche toggle del filtro `movementType` (`aria-pressed`, bordo colorato quando attivo), Saldo del periodo è informativo. Valori da `totalIncomes`/`totalOutcomes`/`totalNet` di `TransactionContext`.
- `ExpenseTable` — wrapper su `DataTableBase` con colonne di `TableContext`; tutte le colonne tranne `actions` sono ordinabili (click sull'intestazione → `TableContext.modifySort`, ordinamento lato server, default data decrescente); lo stato vuoto offre "Nuova transazione" (`useQuickActions().openNewTransaction`).
- `ExpenseTableRow` — data (giorno, mese, anno + giorno della settimana), descrizione, categoria come badge neutro, conto, importo con segno (`formatSignedCurrency`, verde solo per le entrate). Azioni inline Modifica/Elimina visibili all'hover/focus della riga su desktop, sempre visibili su mobile. L'eliminazione usa `ConfirmDeleteDialog` e conferma con `useSuccessMessage().showSuccess`.
- `TransactionModal` — form add/edit montato una sola volta da `QuickActionsProvider`. Selettore Uscita/Entrata (`ToggleButtonGroup`) + importo sempre positivo: il segno viene applicato in `modifyAmount` in base al tipo. L'importo è tenuto come testo locale per permettere input parziali ("0,", "0.5"). Categoria da `allCategories` (etichetta `name`), conto obbligatorio, `DatePicker` in locale `it`.
- `TransferModal` — form nuovo trasferimento, montato da `QuickActionsProvider` e aperto con `openNewTransfer()`.

## Logica
- Query orchestrate da `TableContext` + `TransactionContext`; caricamento gated da `location.pathname === "/movimenti"`.
- CRUD via `TransactionModalContext.sendTransaction()` → `TransactionContext.addTransaction`/`updateTransaction` → `TransactionService.add`/`update`; eliminazione `TransactionContext.deleteTransaction` → `TransactionService.delete(id)`.
- Trasferimenti: `TransferPayload` → `TransactionContext.addTransfer()` → `TransferService.add()`.
- Dopo ogni add/update/delete/transfer il contesto ricarica i mesi disponibili, incrementa `dataVersion` e rilancia `refreshTransactions()`.
