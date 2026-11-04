# 08 - Transactions flow (Tecnico)

## Schermata
`src/app/features/transactions/transactions-page.ts/.html` (route `/movimenti`): `PageHeader` con `NewEntryButton`, poi `TransactionFilters` e la tabella (`DataTable`). Solo elenco con filtri, ordinamento e paginazione (nessun riepilogo di totali). La dashboard mostra gli ultimi movimenti in sola lettura (modulo 11). All'apertura la pagina chiama `TransactionsStore.activatePage()` e `TransactionsTableStore.activate()`; le disattivazioni sono registrate su `DestroyRef`.

## Componenti chiave
- `TransactionFilters` (`transaction-filters.ts/.html/.scss`) — due righe di controlli alti 40px: periodo (a sinistra) e tipo di movimento (a destra da 900px); poi i multi-select conti/categorie a larghezza uguale, "Azzera filtri" e "Aggiorna" (su una riga da 600px; su mobile ogni controllo occupa la riga intera):
  - periodo: `mat-button-toggle-group` (classe `segmented`) Mese/Personalizzato (`periodMode`). **Mese**: frecce precedente/successivo (i mesi arrivano dal più recente) e `mat-select` dei mesi; il filtro copre il mese intero. **Personalizzato**: due input con `mat-datepicker` (adapter date-fns, locale `it`, `min`/`max` reciproci) nello stesso riquadro, preceduti da "Dal"/"Al"; scrivono `customRange` (`YYYY-MM-DD`, estremo vuoto = nessun limite). Passando a Personalizzato senza intervallo, questo parte dal mese selezionato;
  - tipo di movimento: Tutti/Entrate/Uscite (`movementType`);
  - `MultiSelectFilter` (`multi-select-filter.ts`, `mat-select multiple`): senza filtro (array vuoto) tutte le opzioni risultano spuntate e un click ne toglie una; in cima al pannello un'intestazione con il conteggio ("N di M") e "Seleziona tutti" che riporta all'array vuoto. Selezionare tutto o togliere l'ultima voce equivale a nessun filtro;
  - "Azzera filtri" (visibile se tipo, conti o categorie sono filtrati; non tocca il periodo) e "Aggiorna" (`refreshTransactions()`).
- Tabella — `DataTable` con le colonne di `TransactionsTableStore`; tutte tranne `actions` ordinabili (click sull'intestazione → `modifySort`, ordinamento lato server, default data decrescente); paginatore 10/20/50; stato vuoto con "Nuova transazione". Le celle (template proiettato) mostrano data (giorno, mese, anno + giorno della settimana), descrizione, categoria come badge neutro, conto, importo con segno (`formatSignedCurrency`, verde solo per le entrate) e le azioni Modifica/Elimina, visibili all'hover/focus della riga su desktop e sempre su mobile. Elimina apre un unico `ConfirmDeleteDialog` a livello di pagina; la conferma mostra "Transazione eliminata".
- `TransactionModal` (`transaction-modal.ts`) — form add/edit montato una sola volta dalla shell e pilotato da `QuickActionsService`. Selettore Uscita/Entrata + importo sempre positivo: il segno è applicato in base al tipo; l'importo è tenuto come testo locale per permettere input parziali. Descrizione, data (`mat-datepicker`), categoria (`EntityAutocomplete` su `CategoriesStore.all`, etichetta `name`) e conto obbligatorio (`EntityAutocomplete` su `AccountsStore.all`).
- `TransferModal` (`transfer-modal.ts`) — nuovo trasferimento, aperto con `openNewTransfer()`.

## Logica
- Query orchestrate da `TransactionsTableStore` (richiesta `computed` + `effect`) e `TransactionsStore`; caricamento solo a pagina Movimenti attiva.
- CRUD: `TransactionModal` → `TransactionsStore.addTransaction`/`updateTransaction` → `TransactionApi.add`/`update`; eliminazione `TransactionsStore.deleteTransaction` → `TransactionApi.delete(id)`.
- Trasferimenti: `TransferPayload` → `TransactionsStore.addTransfer()` → `TransactionApi.addTransfer()`.
- Dopo ogni add/update/delete/transfer lo store incrementa `dataVersion` e, se la pagina è aperta, ricarica i mesi disponibili e rilancia `refreshTransactions()`.
