# 07 - Global state contexts (Tecnico)

## Context principali
- `AccountContext`
- `CategoryContext`
- `TransactionContext`
- `TableContext`
- `TransactionModalContext`

## Pattern usato
- `Provider + custom hook` per accesso stato/azioni.
- Refresh dati dopo operazioni CRUD.

## Dipendenze
`TableContext` dipende da account, categorie e transazioni per costruire filtri e query.
