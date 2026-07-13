# 04 - Authentication MSAL (Tecnico)

## Configurazione
- `src/auth/msalInstance.ts` crea l'istanza singleton `PublicClientApplication` (`msalInstance`), condivisa tra `MsalProvider` (bootstrap, modulo 02) e `src/services/ApiClient.ts`.
- `src/config/authConfig.ts` costruisce `msalConfig` (authority `https://login.microsoftonline.com/{tenantId}`, `redirectUri`/`postLogoutRedirectUri` = origin, cache in `sessionStorage`), `loginRequest` (scope singolo `appEnv.msalApiScope`) e la factory `apiTokenRequest(account)` per il token silenzioso (stesso scope).
- Variabili richieste in `src/config/env.ts`: `VITE_MSAL_CLIENT_ID`, `VITE_MSAL_TENANT_ID`, `VITE_MSAL_API_SCOPE` (oltre a `VITE_EXPENSE_TRACKER_API_BASE_URL`).

## Flusso
1. `src/App.tsx` avvolge l'intera `AppShell` in `MsalAuthenticationTemplate` (`interactionType={InteractionType.Redirect}`, `authenticationRequest={loginRequest}`) → login redirect con lo scope API.
2. Acquisizione token silente in `src/services/ApiClient.ts` (`getAccessToken`): recupera l'account attivo con `msalInstance.getActiveAccount()` (fallback al primo account in `getAllAccounts()`) e chiama `acquireTokenSilent(apiTokenRequest(account))`.
3. Se l'acquisizione silente fallisce, fallback a `msalInstance.acquireTokenRedirect(apiTokenRequest(account))` (redirect completo); la chiamata API in corso prosegue senza header `Authorization` (la funzione ritorna `null`).

## Error handling auth
- `MsalAuthenticationTemplate` usa `src/pages/AuthErrorPage.tsx` come `errorComponent` di fallback: mostra `error?.message` (o un messaggio generico) e un pulsante "Riprova" che richiama `instance.loginRedirect(loginRequest)`.
