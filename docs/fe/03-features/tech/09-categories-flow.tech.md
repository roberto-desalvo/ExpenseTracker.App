# 09 - Categories flow (Tecnico)

## Schermata
`src/app/features/settings/categories-settings.ts/.html/.scss` gestisce il CRUD delle categorie. È mostrata da `SettingsPage` nella sezione `?sezione=categorie` (default di `/impostazioni`, modulo 03). Le analisi per categoria sono nella dashboard e nella pagina Analisi (modulo 11).

Le categorie sono **per utente** lato backend: gli endpoint `api/category/*` restituiscono e modificano solo quelle dell'utente autenticato, quindi la pagina non filtra nulla in più. Per l'utente demo, `POST api/demo/generate` (non chiamato dal frontend) riporta le categorie a quelle di default.

## Dipendenze
- Stato: `CategoriesStore` (`paged`, `all`, `page`, `pageSize`, `totalCount`, `isLoading`, `addCategory`, `updateCategory`, `deleteCategory`, `refreshPaged`, `syncTransactions`); la lista paginata è attivata nel costruttore con `activatePagedList()`.
- API: `CategoryApi` (`getAll`, `add`, `update`, `delete`, `syncTransactions`).
- UI: `SearchField`, `DataTable`, `EditorDrawer`, `ConfirmDeleteDialog` (`src/app/shared/ui/`), `CategorySyncModal` (`features/settings/category-sync-modal.ts`), `NotificationService.showSuccess` per le conferme.

## Barra superiore
`SearchField`: ricerca per nome con debounce di 350ms (scatta con campo vuoto o da 3 caratteri, con hint "Digita almeno 3 caratteri"), pulsante di cancellazione; a destra "Sincronizza categorie" (apre `CategorySyncModal`) e il bottone primario "Nuova categoria".

## Tabella
Colonne: `Categoria` (avatar con iniziali, nome, badge "Predefinita" con lucchetto se `isDefault`, descrizione sotto il nome), `Parole chiave` (tutti i tag come chip che vanno a capo; la cella ha altezza massima ~3 righe e oltre scorre in verticale; tag lunghi troncati con ellissi e testo intero nel `title`), `Priorità` (a destra), azioni. `tableLayout="fixed"` rispetta le larghezze percentuali delle colonne (niente scroll orizzontale oltre `minTableWidth` 640px). Click sulla riga = modifica; le icone Modifica/Elimina compaiono all'hover. Elimina è disabilitata, con tooltip esplicativo, per la categoria predefinita.

## Editor (`EditorDrawer`)
Pannello laterale destro (larghezza 440px, tutto lo schermo su mobile) con: Nome (obbligatorio, salvataggio disabilitato se vuoto), Descrizione (multilinea), Parole chiave (`mat-chip-grid` con `matChipInput`: Invio o virgola aggiungono il tag; `mat-autocomplete` suggerisce i tag già usati in `CategoriesStore.all`; hint che spiega l'uso nell'import e nella sincronizzazione), Priorità (numerica, hint: in caso di più corrispondenze vince il numero più basso, coerente con `OrderBy(Priority)` dei servizi di import BE). In modifica di una categoria non predefinita il footer ha anche "Elimina". `isDefault` è `false` in creazione e preservato in modifica.

## Sincronizzazione (`CategorySyncModal`)
`AppModal` con due input `type="date"` "Dal"/"Al" (default: 1° gennaio dell'anno corrente → oggi, ripristinati a ogni apertura) e un avviso se l'inizio segue la fine (in quel caso il submit non parte e la modale resta aperta). Il submit chiama `CategoriesStore.syncTransactions({ startDate, endDate })` → `POST api/category/sync`, con date `yyyy-MM-dd` senza orario (il BE considera solo il giorno). L'esito (`CategorySyncResult`) è mostrato come notifica di successo ("N movimenti riclassificati su M analizzati"). La regola (tag, priorità, esclusione trasferimenti, movimenti senza match invariati) è lato BE: vedi `docs/be/02-data-persistence/tech/07-application-services-and-mapping.tech.md`.

## Eliminazione
`ConfirmDeleteDialog` con messaggio che avvisa della riassegnazione dei movimenti alla categoria predefinita.
