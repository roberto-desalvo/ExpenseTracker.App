# 02 - Bootstrap and providers (Tecnico)

## Flusso di avvio
1. `src/main.tsx` crea/importa il singleton MSAL (`src/auth/msalInstance.ts`) e chiama `msalInstance.initialize()`.
   Con il bypass di sviluppo (`appEnv.devAuthBypass`, modulo 04) `msalInstance` è `null`: l'app viene montata subito, senza `MsalProvider`.
2. Solo dopo la risoluzione della Promise, l'app viene montata con `createRoot(...).render(...)` dentro i provider globali (gestisce il redirect MSAL prima del primo render).
3. `App.tsx` avvolge tutto in `BrowserRouter` e renderizza `AppShell`, che applica `AuthGate` (template MSAL o login di sviluppo) e i provider di dominio prima di mostrare header e route.

## Catena provider
Ordine effettivo dall'esterno verso l'interno.

In `src/main.tsx`:
- `MsalProvider` (istanza `msalInstance`; assente con il bypass di sviluppo)
- `AppThemeProvider`
- `ApiErrorProvider`
- `SuccessMessageProvider`
- `App` (che contiene `BrowserRouter`)

In `src/App.tsx`, dentro `AppShell`:
- `AuthGate` → `MsalAuthenticationTemplate` (`InteractionType.Redirect`, `errorComponent={AuthErrorPage}`) + `AuthContext`, oppure `DevLoginPage` + `AuthContext` con il bypass di sviluppo
- `AccountsProvider`
- `TransactionsProvider`
- `CategoriesProvider`
- `TableContextProvider`
- `TransactionModalProvider`
- `QuickActionsProvider` (monta una sola volta `TransactionModal` e `TransferModal`, apribili da ogni pagina)
- contenuto: `AppLayout` (shell con barra laterale o superiore) + `Routes` (dettaglio nel modulo 03)

## Motivazione
Centralizza autenticazione, tema e notifiche in alto nell'albero React, prima ancora del routing. I provider di dominio (account, transazioni, categorie, tabella, modale transazione) vengono inizializzati subito dopo l'autenticazione, dentro l'area protetta, così sono disponibili a tutte le pagine.

## File sorgente
- `src/main.tsx`
- `src/App.tsx`
- `src/auth/msalInstance.ts`, `src/auth/AuthGate.tsx`, `src/auth/AuthContext.ts`, `src/auth/devAuth.ts`
- `src/stores/ThemeContext.tsx`
- `src/stores/ApiErrorContext.tsx`
- `src/stores/SuccessMessageContext.tsx`
- `src/stores/AccountContext.tsx`, `TransactionContext.tsx`, `CategoryContext.tsx`, `TableContext.tsx`, `TransactionModalContext.tsx`, `QuickActionsContext.tsx`
