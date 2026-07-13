# 09 - Categories flow (Tecnico)

## Schermata
`src/pages/CategoriesPage.tsx` gestisce solo il CRUD delle categorie: non esiste più una vista analitica/tab Gestione-Analisi in questa pagina (il trend storico per categoria è mostrato altrove, nella sezione "Patrimonio" della dashboard — vedi modulo `11-dashboard-and-time-series`).

La pagina accetta una prop `embedded?: boolean` (default `false`). Non è più raggiungibile come route standalone: `src/App.tsx` non definisce più `/categorie`. Viene montata unicamente da `src/pages/SettingsPage.tsx` come tab "Categorie" della route `/impostazioni` (`?tab=categorie`), invocata con `<CategoriesPage embedded />` (vedi modulo `03-routing-and-shell` per il dettaglio del routing/tab). Quando `embedded` è `true`, la pagina sopprime il proprio titolo `Typography` "Categorie" e azzera il padding esterno (`px`/`py` a 0 invece di `{xs:2.5, md:4}`/`{xs:2.5, md:3}`).

## Dipendenze
- Stato: `CategoryContext` (`categories`, `page`, `pageSize`, `totalCount`, `isLoading`, `addCategory`, `updateCategory`, `deleteCategory`, `refreshCategories`).
- API: `CategoryService` (`getAll`, `add`, `update`, `delete`); nessuna dipendenza diretta da `TransactionService` in questa pagina.
- UI: `DataTableBase`, `RowActionsMenu`, `CategoriesFilterBar`, `AppModal`, `ConfirmDeleteDialog`.

## Colonne tabella
`Nome` (con badge "Default" se `category.isDefault`), `Descrizione`, `Tag`, `Priorità` (allineata a destra), `Azioni`.

## Filtro/ricerca
`CategoriesFilterBar` espone: campo di ricerca per nome con debounce di 350ms (scatta se il termine è vuoto oppure ha almeno 3 caratteri), pulsante "Nuova categoria" (apre `AppModal` in modalità creazione) e pulsante "Aggiorna" (`refreshCategories()`).

## Form categoria (`AppModal`)
Campi: Nome (obbligatorio), Descrizione, Tag (stringa separata da virgola/punto e virgola, parsata in array), Priorità (numerico). In creazione `isDefault` è sempre `false`; in modifica viene preservato il valore esistente.

## Eliminazione
Azione "Elimina" disabilitata per le categorie con `isDefault === true` (sia nel menu azioni sia lato handler `handleDeleteRequest`). Conferma tramite `ConfirmDeleteDialog`, con messaggio che avvisa che le transazioni collegate verranno riassegnate alla categoria di default.
