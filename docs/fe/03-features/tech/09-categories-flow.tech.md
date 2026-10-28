# 09 - Categories flow (Tecnico)

## Schermata
`src/pages/CategoriesPage.tsx` gestisce il CRUD delle categorie. È montata da `SettingsPage` nella sezione `?sezione=categorie` (default di `/impostazioni`, vedi modulo 03). Le analisi per categoria sono nella dashboard e nella pagina Analisi (modulo 11).

Le categorie sono **per utente** lato backend: gli endpoint `api/category/*` restituiscono e modificano solo quelle dell'utente autenticato, quindi la pagina non filtra nulla in più. Per l'utente demo, `POST api/demo/generate` (non chiamato dal frontend) riporta le categorie a quelle di default.

## Dipendenze
- Stato: `CategoryContext` (`categories`, `allCategories`, `page`, `pageSize`, `totalCount`, `isLoading`, `addCategory`, `updateCategory`, `deleteCategory`, `refreshCategories`).
- API: `CategoryService` (`getAll`, `add`, `update`, `delete`, `syncTransactions`).
- UI: `SearchField`, `DataTableBase`, `EditorDrawer`, `ConfirmDeleteDialog` (`src/components/ui/` e `src/components/`), `CategorySyncModal` (`src/components/settings/`), `useSuccessMessage` per le conferme.

## Barra superiore
`SearchField`: ricerca per nome con debounce di 350ms (scatta con campo vuoto o da 3 caratteri, con helper "Digita almeno 3 caratteri"), pulsante di cancellazione; a destra il bottone secondario "Sincronizza categorie" (apre `CategorySyncModal`) e il bottone primario "Nuova categoria".

## Tabella
Colonne: `Categoria` (avatar con iniziali, nome, chip "Predefinita" con lucchetto se `isDefault`, descrizione sotto il nome), `Parole chiave` (tutti i tag come chip che vanno a capo; la cella ha `maxHeight` ~3 righe e oltre scorre in verticale; le chip troppo lunghe si troncano con ellissi e mostrano il testo intero al passaggio del mouse), `Priorità` (a destra), azioni. La tabella usa `tableLayout="fixed"` di `DataTableBase`, così le larghezze percentuali delle colonne sono rispettate e tag, nomi o descrizioni lunghi non allargano la tabella (niente scroll orizzontale oltre `minTableWidth` 640px). Click sulla riga = modifica; le icone Modifica/Elimina compaiono all'hover. Elimina è disabilitata, con tooltip esplicativo, per la categoria predefinita.

## Editor (`EditorDrawer`)
Pannello laterale destro (a schermo intero su mobile) con: Nome (obbligatorio, il salvataggio è disabilitato se vuoto), Descrizione (multilinea), Parole chiave (`Autocomplete` multiplo `freeSolo` con suggerimenti dai tag già usati in `allCategories`; helper che spiega l'uso nell'import e nella sincronizzazione), Priorità (numerica, helper: in caso di più corrispondenze vince il numero più basso, coerente con l'ordinamento `OrderBy(Priority)` dei servizi di import BE). In modifica di una categoria non predefinita il footer ha anche "Elimina". `isDefault` è `false` in creazione e preservato in modifica.

## Sincronizzazione (`CategorySyncModal`)
`AppModal` con due `TextField type="date"` "Dal"/"Al" (default: 1° gennaio dell'anno corrente → oggi, ripristinati a ogni apertura) e un avviso se l'inizio segue la fine (in quel caso il submit non parte e la modale resta aperta). Il submit chiama `CategoryService.syncTransactions({ startDate, endDate })` → `POST api/category/sync`, inviando le date come `yyyy-MM-dd` senza orario per evitare slittamenti di fuso (il BE considera solo il giorno). L'esito (`CategorySyncResult { examinedTransactions, updatedTransactions, unmatchedTransactions }`) è mostrato nello snackbar della modale ("N movimenti riclassificati su M analizzati"). La regola (tag, priorità, esclusione trasferimenti, movimenti senza match invariati) è tutta lato BE: vedi `docs/be/02-data-persistence/tech/07-application-services-and-mapping.tech.md`.

## Eliminazione
`ConfirmDeleteDialog` con messaggio che avvisa della riassegnazione dei movimenti alla categoria predefinita.
