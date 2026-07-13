# 08 - Transactions flow (Tecnico)

## Schermata
`src/pages/TransactionsPage.tsx` con tab Gestione/Analisi (analisi placeholder).

## Componenti chiave
- `TransactionsSummaryBar`
- `AccountsBar`
- `ExpenseTable`
- `TransactionModal`

## Logica
- Query orchestrate da `TableContext` + `TransactionContext`.
- CRUD transazioni via `TransactionModalContext` e `TransactionService`.
