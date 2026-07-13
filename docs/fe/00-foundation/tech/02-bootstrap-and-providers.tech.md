# 02 - Bootstrap and providers (Tecnico)

## Flusso di avvio
1. Inizializzazione MSAL in `src/main.tsx`.
2. Render app dentro provider globali.

## Catena provider
- `MsalProvider`
- `AppThemeProvider`
- `ApiErrorProvider`
- `SuccessMessageProvider`

## Motivazione
Centralizza autenticazione, tema e notifiche in alto nell'albero React.

## File sorgente
- `src/main.tsx`
- `src/stores/ThemeContext.tsx`
- `src/stores/ApiErrorContext.tsx`
- `src/stores/SuccessMessageContext.tsx`
