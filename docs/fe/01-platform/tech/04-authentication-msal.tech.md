# 04 - Authentication MSAL (Tecnico)

## Configurazione
- `src/app/core/auth/msalInstance.ts` crea il singleton `PublicClientApplication` (`msalInstance`, `null` con il bypass di sviluppo), usato da `AuthService` e dall'interceptor HTTP.
- `src/app/core/auth/authConfig.ts` costruisce `msalConfig` (authority `https://login.microsoftonline.com/{tenantId}`, `redirectUri`/`postLogoutRedirectUri` = origin, cache in `sessionStorage`), `loginRequest` (scope `appEnv.msalApiScope`) e `apiTokenRequest(account)` per il token silenzioso.
- Variabili in `src/app/core/config/env.ts`: `VITE_MSAL_CLIENT_ID`, `VITE_MSAL_TENANT_ID`, `VITE_MSAL_API_SCOPE` (oltre a `VITE_EXPENSE_TRACKER_API_BASE_URL`); non richieste quando `appEnv.devAuthBypass` è attivo.
- Si usa `@azure/msal-browser` direttamente (non `msal-angular`): le tre modalità di accesso (Entra ID, demo, sviluppo) sono gestite in un solo punto.

## `AuthService` (`src/app/core/auth/auth.service.ts`)
Stato a signal indipendente dal provider: `user()` (`{ mode: "dev" | "demo" | "msal"; userName }` o `null`), `isAuthenticated()`, `userName()`, `loginError()`, più i flag `devBypass` e `demoLoginEnabled`. Azioni: `initialize()`, `loginDev(name)`, `loginEntra()`, `loginDemo()`, `logout()`. La shell lo usa per nome utente e logout, la dashboard per il saluto.

## Flusso
1. `initialize()` gira in `provideAppInitializer` (modulo 02), prima del render:
   - `msalInstance.initialize()`, registrazione di un event callback: in MSAL v5 anche il login redirect fallito arriva come `ACQUIRE_TOKEN_FAILURE`, il cui messaggio finisce in `loginError()`;
   - `handleRedirectPromise()` (ritorno da Entra ID): l'account restituito diventa quello attivo; un'eccezione finisce in `loginError()`;
   - sessione demo attiva (`hasDemoSession()`) → utente `demo`; altrimenti account attivo o primo di `getAllAccounts()` → utente `msal`.
2. `App` mostra `LoginPage` (`src/app/pages/auth/login-page.ts`) se non c'è utente: "Accedi con Microsoft" (`loginRedirect(loginRequest)`) e, se `appEnv.demoLoginEnabled`, "Prova la demo". Gli errori di login (redirect o demo) compaiono nella pagina stessa, da cui si può riprovare (non esiste più una pagina d'errore separata).
3. Token per le API: `getAuthorizationHeader()` in `src/app/core/auth/auth-header.ts`, usato da `authInterceptor` (modulo 05). Con MSAL recupera l'account attivo (fallback al primo) e chiama `acquireTokenSilent(apiTokenRequest(account))`; se fallisce esegue `acquireTokenRedirect` e la richiesta in corso parte senza `Authorization`.
4. `logout()`: MSAL → `logoutRedirect()`; demo → `clearDemoSession()`; sviluppo → `clearDevUser()`. Negli ultimi due casi si torna subito alla pagina di login.

## Login demo (utente di test)
- `src/app/core/auth/demoAuth.ts` — `startDemoSession()` chiama `POST {apiBaseUrl}/api/auth/demo` (`apiConfig.auth.demo`, `fetch` senza header `Authorization` né interceptor) e salva `{ accessToken, expiresOn, userName }` in `sessionStorage` (`expense-tracker:demo-session`) e in memoria. `getDemoAccessToken()` rinnova il token (nuova chiamata) quando mancano meno di 60 s alla scadenza; chiamate concorrenti condividono la stessa richiesta. 404 → "La demo non è al momento disponibile."
- Con una sessione demo l'header è `Authorization: Bearer <token demo>` invece del token MSAL; `userName` è quello restituito dal BE.
- `appEnv.demoLoginEnabled` = `VITE_DEMO_LOGIN_ENABLED !== "false"`: pulsante visibile di default, da nascondere dove il BE non ha `DemoAuth__Enabled=true`.

## Bypass di sviluppo (login con solo nome utente)
- Attivo quando `appEnv.devAuthBypass` = `DEV && VITE_DEV_AUTH_BYPASS !== "false"`, dove `DEV` è scritto da `scripts/write-env.mjs` solo per `npm start` (mai nelle build); `VITE_DEV_AUTH_BYPASS=false` forza MSAL anche in locale.
- MSAL non viene creato né inizializzato; `App` mostra `src/app/pages/auth/dev-login-page.ts` (campo "Nome utente", max 60 caratteri) finché non c'è un utente. Il nome è in `sessionStorage` (`expense-tracker:dev-user`, `src/app/core/auth/devAuth.ts`).
- L'interceptor invia `Authorization: DevUser <username>`. Il BE lo accetta solo con `DevAuth__Enabled=true` in `Development` (vedi `docs/be/01-platform/tech/04-auth-and-request-pipeline.tech.md`).
