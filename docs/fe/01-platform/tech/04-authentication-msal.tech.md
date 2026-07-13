# 04 - Authentication MSAL (Tecnico)

## Configurazione
- `src/config/authConfig.ts` costruisce `msalConfig`, `loginRequest`, `apiTokenRequest`.
- Variabili richieste in `src/config/env.ts`.

## Flusso
1. Login redirect con scope API.
2. Acquisizione token silente in `src/services/ApiClient.ts`.
3. Fallback redirect se token silent fallisce.

## Error handling auth
- `MsalAuthenticationTemplate` usa `AuthErrorPage` come fallback UI.
