# 05 - API integration and errors (Tecnico)

## Endpoint registry
`src/config/api.ts` centralizza gli URL REST in `apiConfig`, costruiti a partire da `appEnv.apiBaseUrl` (`src/config/env.ts`):
- `accounts`: `base`, `query`
- `transactions`: `base`, `query`, `monthOptions`, `landing`, `series`, `stock`, `byId(id)`
- `categories`: `base`, `query`, `byId(id)`
- `transfers`: `base`

`VITE_EXPENSE_TRACKER_API_BASE_URL` deve essere un URL assoluto (`https://...`): senza schema il browser lo risolverebbe come percorso relativo all'origine del frontend. `normalizeBaseUrl` in `env.ts` rimuove gli slash finali e lancia un errore all'avvio se manca `http(s)://`; il workflow `deploy-frontend.yml` blocca il deploy se il secret non inizia con `https://`.

## Client condiviso
`src/services/ApiClient.ts` espone:
- `apiFetchJson<T>(url, init, fallbackMessage)`
- `apiFetchVoid(url, init, fallbackMessage)`

Entrambe iniettano l'header `Authorization: Bearer <token>` tramite `withAuthHeader`/`getAccessToken` (token silenzioso MSAL, vedi modulo 04) prima di eseguire la `fetch`.

`apiFetchJson` è usata solo per letture (anche le query in `POST`; le scritture passano da `apiFetchVoid`) e deduplica le richieste in volo: la mappa `inFlightRequests` è indicizzata per `metodo + url + body` (solo body stringa) e una richiesta identica a una ancora pendente riceve la stessa promise, che viene rimossa dalla mappa al completamento. Così il doppio effect di `StrictMode` in sviluppo o più componenti che chiedono lo stesso dato insieme producono una sola chiamata HTTP (ed eventualmente una sola notifica d'errore).

## Gestione errori
- Se la response non è `ok`, `buildErrorMessage` estrae il messaggio dal body JSON: prova prima `detail`, poi `title`, poi il primo valore in `errors` (dizionario campo→messaggi); se non estrae nulla usa il testo grezzo del body o `${fallbackMessage} (HTTP {status})`.
- Il messaggio viene emesso come evento globale `window` `expense-tracker:api-error` (costante `API_ERROR_EVENT`) e poi rilanciato come eccezione (`ApiRequestError`, con flag interno `alreadyNotified` per evitare doppie notifiche se l'errore risale nello stack).
- `src/stores/ApiErrorContext.tsx` (`ApiErrorProvider`) resta in ascolto dell'evento e mostra un MUI `Snackbar`/`Alert` di severità `error` (auto-hide 5s); espone anche l'hook `useApiError()` (`showError(message)`) per notifiche manuali al di fuori del flusso HTTP.
