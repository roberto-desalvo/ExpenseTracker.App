# 10 - Accounts and transfers flow (Tecnico)

## Accounts
- Pagina: `src/pages/AccountsPage.tsx`
- Stato: `AccountContext`
- API: `AccountService`

## Transfers
- Operazione tramite `TransferService` invocata da `TransactionContext.addTransfer()`.

## Analisi account
`AccountsPage` integra anche grafico trend via `TransactionService.getStock()`.
