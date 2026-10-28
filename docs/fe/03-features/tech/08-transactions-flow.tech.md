# 08 - Transactions flow (Tecnico)

## Schermata
`src/pages/TransactionsPage.tsx` (route `/movimenti`): `PageHeader` con `NewEntryButton`, poi `TransactionFilters` ed `ExpenseTable`: la pagina è solo elenco con filtri, ordinamento e paginazione (nessun riepilogo di totali). La dashboard mostra solo gli ultimi movimenti in sola lettura (modulo 11).

## Componenti chiave
- `TransactionFilters` (`src/components/TransactionFilters.tsx`) — due righe di controlli alti 40px: la prima con periodo (a sinistra) e tipo di movimento (a destra da `md`), la seconda con i multi-select conti/categorie a larghezza uguale, "Azzera filtri" e "Aggiorna" (senza andare a capo da `sm`; su mobile ogni controllo occupa l'intera riga). Nel dettaglio:
  - periodo: `ToggleButtonGroup` Mese/Personalizzato (`TableContext.periodMode`). In modalità **Mese** selettore con frecce precedente/successivo (i mesi di `TableContext.availableMonths` arrivano dal più recente) e `Select` dei mesi; il filtro copre il mese intero (`startDate`–`endDate` dell'opzione, dal 1° all'ultimo giorno). In modalità **Personalizzato** due `DatePicker` (locale `it`, variante `standard` senza sottolineatura, `minDate`/`maxDate` reciproci) affiancati nello stesso riquadro del selettore mese, preceduti dalle etichette "Dal"/"Al"; svuotare il campo toglie il limite, che scrivono `TableContext.customRange` (`YYYY-MM-DD`, estremi inclusi; un estremo vuoto = nessun limite da quel lato). Passando a Personalizzato senza intervallo impostato, questo parte dal mese selezionato;
  - tipo di movimento: `ToggleButtonGroup` Tutti/Entrate/Uscite (`movementType`);
  - multi-select conti (`accounts`) e categorie (`allCategories`): senza filtro (array vuoto) tutte le opzioni risultano spuntate e un click ne toglie una; nessuna voce "Tutti" nell'elenco, ma un `ListSubheader` in cima al menu con il conteggio ("N di M") e il bottone "Seleziona tutti" che riporta all'array vuoto. Selezionare tutte le opzioni o togliere l'ultima equivale a nessun filtro;
  - "Azzera filtri" (visibile se tipo, conti o categorie sono filtrati; non tocca il periodo) e bottone "Aggiorna".
- `ExpenseTable` — wrapper su `DataTableBase` con colonne di `TableContext`; tutte le colonne tranne `actions` sono ordinabili (click sull'intestazione → `TableContext.modifySort`, ordinamento lato server, default data decrescente); lo stato vuoto offre "Nuova transazione" (`useQuickActions().openNewTransaction`).
- `ExpenseTableRow` — data (giorno, mese, anno + giorno della settimana), descrizione, categoria come badge neutro, conto, importo con segno (`formatSignedCurrency`, verde solo per le entrate). Azioni inline Modifica/Elimina visibili all'hover/focus della riga su desktop, sempre visibili su mobile. L'eliminazione usa `ConfirmDeleteDialog` e conferma con `useSuccessMessage().showSuccess`.
- `TransactionModal` — form add/edit montato una sola volta da `QuickActionsProvider`. Selettore Uscita/Entrata (`ToggleButtonGroup`) + importo sempre positivo: il segno viene applicato in `modifyAmount` in base al tipo. L'importo è tenuto come testo locale per permettere input parziali ("0,", "0.5"). Categoria da `allCategories` (etichetta `name`), conto obbligatorio, `DatePicker` in locale `it`.
- `TransferModal` — form nuovo trasferimento, montato da `QuickActionsProvider` e aperto con `openNewTransfer()`.

## Logica
- Query orchestrate da `TableContext` + `TransactionContext`; caricamento gated da `location.pathname === "/movimenti"`.
- CRUD via `TransactionModalContext.sendTransaction()` → `TransactionContext.addTransaction`/`updateTransaction` → `TransactionService.add`/`update`; eliminazione `TransactionContext.deleteTransaction` → `TransactionService.delete(id)`.
- Trasferimenti: `TransferPayload` → `TransactionContext.addTransfer()` → `TransferService.add()`.
- Dopo ogni add/update/delete/transfer il contesto ricarica i mesi disponibili, incrementa `dataVersion` e rilancia `refreshTransactions()`.
