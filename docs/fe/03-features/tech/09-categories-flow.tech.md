# 09 - Categories flow (Tecnico)

## Schermata
`src/pages/CategoriesPage.tsx` gestisce il CRUD delle categorie. È montata da `SettingsPage` nella sezione `?sezione=categorie` (default di `/impostazioni`, vedi modulo 03). Le analisi per categoria sono nella dashboard e nella pagina Analisi (modulo 11).

## Dipendenze
- Stato: `CategoryContext` (`categories`, `allCategories`, `page`, `pageSize`, `totalCount`, `isLoading`, `addCategory`, `updateCategory`, `deleteCategory`, `refreshCategories`).
- API: `CategoryService` (`getAll`, `add`, `update`, `delete`).
- UI: `SearchField`, `DataTableBase`, `EditorDrawer`, `ConfirmDeleteDialog` (`src/components/ui/` e `src/components/`), `useSuccessMessage` per le conferme.

## Barra superiore
`SearchField`: ricerca per nome con debounce di 350ms (scatta con campo vuoto o da 3 caratteri, con helper "Digita almeno 3 caratteri"), pulsante di cancellazione; a destra il bottone primario "Nuova categoria".

## Tabella
Colonne: `Categoria` (avatar con iniziali, nome, chip "Predefinita" con lucchetto se `isDefault`, descrizione sotto il nome), `Parole chiave import` (tag come chip, max 5 + contatore), `Priorità` (a destra), azioni. Click sulla riga = modifica; le icone Modifica/Elimina compaiono all'hover. Elimina è disabilitata, con tooltip esplicativo, per la categoria predefinita.

## Editor (`EditorDrawer`)
Pannello laterale destro (a schermo intero su mobile) con: Nome (obbligatorio, il salvataggio è disabilitato se vuoto), Descrizione (multilinea), Parole chiave (`Autocomplete` multiplo `freeSolo` con suggerimenti dai tag già usati in `allCategories`; helper che spiega l'uso nell'import), Priorità (numerica, helper: in caso di più corrispondenze vince il numero più basso, coerente con l'ordinamento `OrderBy(Priority)` dei servizi di import BE). In modifica di una categoria non predefinita il footer ha anche "Elimina". `isDefault` è `false` in creazione e preservato in modifica.

## Eliminazione
`ConfirmDeleteDialog` con messaggio che avvisa della riassegnazione dei movimenti alla categoria predefinita.
