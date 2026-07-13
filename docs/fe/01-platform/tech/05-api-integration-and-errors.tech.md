# 05 - API integration and errors (Tecnico)

## Endpoint registry
`src/config/api.ts` centralizza URL REST per accounts, transactions, categories, transfers.

## Client condiviso
`src/services/ApiClient.ts` espone:
- `apiFetchJson<T>()`
- `apiFetchVoid()`

## Gestione errori
- Parsing messaggi backend (`detail`, `title`, `errors`).
- Emissione evento globale `expense-tracker:api-error`.
- UI snackbar tramite `ApiErrorProvider`.
