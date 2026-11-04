# 05 - API integration and errors (Tecnico)

## Endpoint registry
`src/app/core/config/api.ts` centralizza gli URL REST in `apiConfig`, costruiti da `appEnv.apiBaseUrl` (`src/app/core/config/env.ts`):
- `auth`: `demo`
- `accounts`: `base`, `query`
- `transactions`: `base`, `query`, `monthOptions`, `landing`, `series`, `stock`, `byId(id)`
- `categories`: `base`, `query`, `sync`, `byId(id)`
- `transfers`: `base`

`VITE_EXPENSE_TRACKER_API_BASE_URL` deve essere un URL assoluto (`https://...`): `normalizeBaseUrl` rimuove gli slash finali e lancia un errore all'avvio se manca `http(s)://`; il workflow `deploy-frontend.yml` blocca il deploy se il secret non inizia con `https://`.

## Client condiviso
`src/app/core/api/api-client.ts` (`ApiClient`, su `HttpClient`):
- `read<T>(method: "GET" | "POST", url, fallbackMessage, body?)` — letture, comprese le query in `POST`. Le richieste identiche ancora in volo (chiave `metodo + url + body serializzato`) condividono la stessa promise, rimossa al completamento: più componenti che chiedono lo stesso dato insieme producono una sola chiamata HTTP (e una sola notifica d'errore).
- `write(method: "POST" | "PUT" | "DELETE", url, fallbackMessage, body?)` — scritture senza corpo di risposta atteso (`responseType: "text"`).
Il messaggio di fallback viaggia nella richiesta come `HttpContextToken` (`ERROR_FALLBACK`).

Servizi API (`@Injectable` root, restituiscono `Promise`): `AccountApi` (`query`, `add`, `update`), `CategoryApi` (`getAll`, `add`, `update`, `delete`, `syncTransactions`), `TransactionApi` (`getAll`, `getMonthOptions`, `getLanding`, `getTimeSeries`, `getStock`, `add`, `update`, `delete`, `addTransfer`). I trasferimenti, prima in un servizio a parte, sono in `TransactionApi.addTransfer`.

## Interceptor (`src/app/core/api/interceptors.ts`)
- `authInterceptor` — solo per URL che iniziano con `apiBaseUrl`: aggiunge `Authorization` da `getAuthorizationHeader()` (`DevUser`, token demo o token MSAL, modulo 04).
- `errorInterceptor` — su errore calcola il messaggio con `extractErrorMessage(body)`: `detail`, poi `title`, poi il primo valore di `errors` (dizionario campo → messaggi); un body testuale viene prima interpretato come JSON, altrimenti usato così com'è; se non si ricava nulla → `${fallback} (HTTP {status})`; errori di rete (status 0) → fallback. Il messaggio viene mostrato con `NotificationService.showError` e rilanciato come `ApiRequestError(message, status)`: i chiamanti fanno solo `catch` per lasciare aperte modali/drawer.

## Notifiche
`src/app/core/api/notification.service.ts` (`NotificationService`) usa `MatSnackBar` in basso a destra: `showError(message)` (rosso, 5 s) e `showSuccess(message)` (verde, 4 s), con classi `app-snack--error`/`--success` in `styles.scss`. Sostituisce i vecchi `ApiErrorContext`/`SuccessMessageContext` e l'evento globale `expense-tracker:api-error`.
