# 09 - Categories flow (Tecnico)

## Schermata
`src/pages/CategoriesPage.tsx` gestisce CRUD categorie e vista analitica.

## Dipendenze
- Stato: `CategoryContext`
- API: `CategoryService`, `TransactionService` (serie storiche)
- UI: `DataTableBase`, `AppModal`, `ConfirmDeleteDialog`, `TimeSeriesLineChart`

## Note
La pagina usa tab Gestione/Analisi analogamente ad Accounts.
