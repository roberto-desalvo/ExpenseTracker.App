# 10 - Accounts and transfers flow (Tecnico)

## Accounts
- Vista: `src/app/features/settings/accounts-settings.ts/.html/.scss`, mostrata da `SettingsPage` nella sezione `?sezione=conti` (accetta anche il vecchio `?tab=account`, modulo 03). CRUD limitato a creazione e modifica (nome, IBAN): non c'è eliminazione (`AccountApi` non ha `delete`).
- Griglia di card (1/2/3 colonne a 600/1200px). Ogni card: barra colorata in alto con il colore categoriale del conto (`buildEntityColorMap`, lo stesso dei grafici), iniziali, nome, IBAN (se presente), saldo attuale e netto del mese corrente, icona "Modifica". L'ultima card tratteggiata "Aggiungi conto" apre la creazione. Paginazione (`Pagination`, `src/app/shared/ui/pagination.ts`) se le pagine sono più di una.
- Saldi: `TransactionApi.getLanding({ excludeTransfers: true })` → `accounts[].currentBalance`/`netMonth` (il `netMonth` per conto include sempre i trasferimenti, indipendentemente da `excludeTransfers`, che vale solo per i totali di flusso); un `effect` ricarica quando cambiano `dataVersion` o l'insieme dei conti, dopo il primo caricamento dell'anagrafica.
- Stato: `AccountsStore` (`paged`, `all`, `page`, `pageSize`, `totalCount`, `isLoading`, `addAccount`, `updateAccount`, `refreshPaged`); lista paginata attivata nel costruttore con `activatePagedList()`.
- Ricerca: `SearchField` (debounce 350ms, da 3 caratteri).
- Editor: `EditorDrawer` con "Nome del conto" (hint: il nome deve coincidere con quello configurato per l'import, perché i servizi di import BE risolvono il conto per nome) e "IBAN" (facoltativo). Prima dell'invio l'IBAN è ripulito dagli spazi e messo in maiuscolo, vuoto → `null`. Validazione e unicità per utente sono lato BE (400/409, mostrati dal layer API). L'update invia sempre `iban`, perché il BE sovrascrive tutti i campi del conto.

## Transfers
- Si creano da qualunque pagina con `NewEntryButton` → "Trasferimento" (`QuickActionsService.openNewTransfer()`), che apre l'unica istanza di `TransferModal` montata nella shell.
- `TransferModal` raccoglie un `TransferPayload` (`fromAccountId`, `toAccountId`, `amount`, `description`, `date`; conti scelti con `EntityAutocomplete`) e invoca `TransactionsStore.addTransfer()` → `TransactionApi.addTransfer()` (`POST api/transfer`). Alla chiusura il form torna ai valori iniziali.
- Dopo l'aggiunta lo store incrementa `dataVersion` e, se la pagina Movimenti è aperta, ricarica mesi e lista.

## Analisi account
Il saldo nel tempo per conto è nella pagina Analisi (grafico "Patrimonio", linee per conto attivabili dalla legenda, modulo 11); la ripartizione del patrimonio per conto è nella dashboard (card "Conti").
