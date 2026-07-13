# 05 - API integration and errors (Tecnico)

## Endpoint registry
`src/config/api.ts` centralizza gli URL REST in `apiConfig`, costruiti a partire da `appEnv.apiBaseUrl` (`src/config/env.ts`):
- `accounts`: `base`, `query`
- `transactions`: `base`, `query`, `monthOptions`, `landing`, `series`, `stock`, `byId(id)`
- `categories`: `base`, `query`, `byId(id)`
- `transfers`: `base`

## Client condiviso
`src/services/ApiClient.ts` espone:
- `apiFetchJson<T>(url, init, fallbackMessage)`
- `apiFetchVoid(url, init, fallbackMessage)`

Entrambe iniettano l'header `Authorization: Bearer <token>` tramite `withAuthHeader`/`getAccessToken` (token silenzioso MSAL, vedi modulo 04) prima di eseguire la `fetch`.

## Gestione errori
- Se la response non è `ok`, `buildErrorMessage` estrae il messaggio dal body JSON: prova prima `detail`, poi `title`, poi il primo valore in `errors` (dizionario campo→messaggi); se non estrae nulla usa il testo grezzo del body o `${fallbackMessage} (HTTP {status})`.
- Il messaggio viene emesso come evento globale `window` `expense-tracker:api-error` (costante `API_ERROR_EVENT`) e poi rilanciato come eccezione (`ApiRequestError`, con flag interno `alreadyNotified` per evitare doppie notifiche se l'errore risale nello stack).
- `src/stores/ApiErrorContext.tsx` (`ApiErrorProvider`) resta in ascolto dell'evento e mostra un MUI `Snackbar`/`Alert` di severità `error` (auto-hide 5s); espone anche l'hook `useApiError()` (`showError(message)`) per notifiche manuali al di fuori del flusso HTTP.
