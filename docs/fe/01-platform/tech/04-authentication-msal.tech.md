# 04 - Authentication MSAL (Tecnico)

## Configurazione
- `src/auth/msalInstance.ts` crea l'istanza singleton `PublicClientApplication` (`msalInstance`, `null` con il bypass di sviluppo), condivisa tra `MsalProvider` (bootstrap, modulo 02) e `src/services/ApiClient.ts`.
- `src/config/authConfig.ts` costruisce `msalConfig` (authority `https://login.microsoftonline.com/{tenantId}`, `redirectUri`/`postLogoutRedirectUri` = origin, cache in `sessionStorage`), `loginRequest` (scope singolo `appEnv.msalApiScope`) e la factory `apiTokenRequest(account)` per il token silenzioso (stesso scope).
- Variabili richieste in `src/config/env.ts`: `VITE_MSAL_CLIENT_ID`, `VITE_MSAL_TENANT_ID`, `VITE_MSAL_API_SCOPE` (oltre a `VITE_EXPENSE_TRACKER_API_BASE_URL`); non richieste quando `appEnv.devAuthBypass` è attivo.
- `src/auth/AuthContext.ts` espone `useAuth()` (`isAuthenticated`, `userName`, `logout`), indipendente dal provider: la shell (`src/components/layout/AppLayout.tsx` e `UserMenuButton` in `HeaderActions.tsx`) lo usa per nome utente e logout, la dashboard per il saluto.

## Flusso
1. `src/App.tsx` avvolge l'intera `AppShell` in `AuthGate` (`src/auth/AuthGate.tsx`). Fuori dal bypass di sviluppo `AuthGate` usa `ProductionAuthGate`, che decide in quest'ordine:
   - sessione demo attiva (`hasDemoSession()`) → `DemoAuthBridge` (vedi sotto);
   - account MSAL già autenticato (`useIsAuthenticated()`) → `MsalAuthenticationTemplate` (`interactionType={InteractionType.Redirect}`, `authenticationRequest={loginRequest}`) + `MsalAuthBridge`;
   - interazione MSAL in corso (`inProgress !== InteractionStatus.None`, es. ritorno dal redirect) → spinner;
   - altrimenti `src/pages/LoginPage.tsx`: "Accedi con Microsoft" (`instance.loginRedirect(loginRequest)`) e, se `appEnv.demoLoginEnabled`, "Prova la demo". Gli errori del login redirect arrivano dall'evento MSAL `ACQUIRE_TOKEN_FAILURE` (MSAL v5 non ha più `LOGIN_FAILURE`) e sono mostrati nella pagina.
2. Acquisizione token silente in `src/services/ApiClient.ts` (`getAccessToken`): recupera l'account attivo con `msalInstance.getActiveAccount()` (fallback al primo account in `getAllAccounts()`) e chiama `acquireTokenSilent(apiTokenRequest(account))`.
3. Se l'acquisizione silente fallisce, fallback a `msalInstance.acquireTokenRedirect(apiTokenRequest(account))` (redirect completo); la chiamata API in corso prosegue senza header `Authorization` (la funzione ritorna `null`).

## Error handling auth
- `MsalAuthenticationTemplate` usa `src/pages/AuthErrorPage.tsx` come `errorComponent` di fallback: mostra `error?.message` (o un messaggio generico) e un pulsante "Riprova" che richiama `instance.loginRedirect(loginRequest)`.

## Login demo (utente di test)
- `src/auth/demoAuth.ts` — `startDemoSession()` chiama `POST {apiBaseUrl}/api/auth/demo` (`apiConfig.auth.demo`, senza header `Authorization`) e salva `{ accessToken, expiresOn, userName }` in `sessionStorage` (`expense-tracker:demo-session`) e in memoria. `getDemoAccessToken()` restituisce il token e lo rinnova in automatico (nuova chiamata a `/auth/demo`) quando mancano meno di 60 s alla scadenza; chiamate concorrenti condividono la stessa richiesta. 404 dal BE → "La demo non è al momento disponibile."
- `ApiClient.getAuthorizationHeader()`: dopo il bypass di sviluppo, se c'è una sessione demo invia `Authorization: Bearer <token demo>` invece del token MSAL.
- `DemoAuthBridge` espone `useAuth()` con `userName` restituito dal BE (`"Demo"`); "Esci" chiama `clearDemoSession()` e torna a `LoginPage` (nessun logout MSAL).
- `appEnv.demoLoginEnabled` (`src/config/env.ts`) = `VITE_DEMO_LOGIN_ENABLED !== "false"`: il pulsante è visibile di default e va nascosto esplicitamente dove il BE non ha `DemoAuth__Enabled=true`.

## Bypass di sviluppo (login con solo nome utente)
- Attivo quando `appEnv.devAuthBypass` = `import.meta.env.DEV && VITE_DEV_AUTH_BYPASS !== "false"`: quindi solo con `npm run dev`, mai nelle build di produzione; `VITE_DEV_AUTH_BYPASS=false` forza MSAL anche in locale.
- `src/main.tsx` non crea né inizializza MSAL e non monta `MsalProvider`.
- `AuthGate` mostra `src/pages/DevLoginPage.tsx` (campo "Nome utente", max 60 caratteri) finché non c'è un utente; il nome è salvato in `sessionStorage` (`expense-tracker:dev-user`, helper in `src/auth/devAuth.ts`). "Esci" lo cancella e torna alla pagina di login.
- `ApiClient` invia `Authorization: DevUser <username>` invece del bearer token. Il BE lo accetta solo con `DevAuth__Enabled=true` in ambiente `Development` (vedi `docs/be/01-platform/tech/04-auth-and-request-pipeline.tech.md`).
