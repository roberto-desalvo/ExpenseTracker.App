# 10 - Accounts and transfers flow (Tecnico)

## Accounts
- Pagina: `src/pages/AccountsPage.tsx`, montata da `SettingsPage` nella sezione `?sezione=conti` (accetta anche il vecchio `?tab=account`, vedi modulo 03). CRUD limitato a creazione e modifica (nome, IBAN): non c'è eliminazione (`AccountService` non ha `delete`).
- Layout a griglia di card (1/2/3 colonne). Ogni card: barra colorata in alto con il colore categoriale del conto (`buildEntityColorMap`, lo stesso usato dai grafici), iniziali, nome, IBAN sotto il nome (se presente), saldo attuale e netto del mese corrente, icona "Modifica". L'ultima card tratteggiata "Aggiungi conto" apre la creazione. Paginazione MUI `Pagination` se `totalCount > pageSize`.
- Saldi: la pagina chiama `TransactionService.getLanding({ excludeTransfers: true })` e usa `accounts[].currentBalance`/`netMonth` (il `netMonth` per conto include sempre i trasferimenti, indipendentemente da `excludeTransfers`, che vale solo per i totali di flusso); ricarica quando cambiano `dataVersion` o il numero di conti.
- Stato: `AccountContext` (`pagedAccounts`, `accounts`, `page`, `pageSize`, `totalCount`, `isLoading`, `addAccount`, `updateAccount`, `refreshAccounts`).
- Ricerca: `SearchField` (debounce 350ms, da 3 caratteri).
- Editor: `EditorDrawer` con i campi "Nome del conto" (helper: il nome deve coincidere con quello configurato per l'import, perché i servizi di import BE risolvono il conto per nome) e "IBAN" (facoltativo). Il modello `Account` ha `iban?: string | null`; prima dell'invio l'IBAN è ripulito dagli spazi e messo in maiuscolo, vuoto → `null`. Validazione e unicità per utente sono lato BE (400/409, mostrati dal layer API). L'update invia sempre `iban`, perché il BE sovrascrive tutti i campi del conto.

## Transfers
- Si creano da qualunque pagina con `NewEntryButton` → "Trasferimento" (`useQuickActions().openNewTransfer()`), che apre l'unica istanza di `TransferModal` montata da `QuickActionsProvider`.
- `TransferModal` raccoglie un `TransferPayload` (`fromAccountId`, `toAccountId`, `amount`, `description`, `date`) e invoca `TransactionContext.addTransfer()` → `TransferService.add()`.
- Dopo l'aggiunta il contesto ricarica i mesi disponibili, incrementa `dataVersion` e rilancia il refresh delle transazioni filtrate.

## Analisi account
Il saldo nel tempo per conto è nella pagina Analisi (grafico "Patrimonio", linee per conto attivabili dalla legenda, modulo 11); la ripartizione del patrimonio per conto è nella dashboard (card "Conti").
