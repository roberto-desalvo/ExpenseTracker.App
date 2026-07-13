# 02 - Bootstrap and providers (Tecnico)

## Flusso di avvio
1. `src/main.tsx` crea/importa il singleton MSAL (`src/auth/msalInstance.ts`) e chiama `msalInstance.initialize()`.
2. Solo dopo la risoluzione della Promise, l'app viene montata con `createRoot(...).render(...)` dentro i provider globali (gestisce il redirect MSAL prima del primo render).
3. `App.tsx` avvolge tutto in `BrowserRouter` e renderizza `AppShell`, che applica il template di autenticazione MSAL e i provider di dominio prima di mostrare header e route.

## Catena provider
Ordine effettivo dall'esterno verso l'interno.

In `src/main.tsx`:
- `MsalProvider` (istanza `msalInstance`)
- `AppThemeProvider`
- `ApiErrorProvider`
- `SuccessMessageProvider`
- `App` (che contiene `BrowserRouter`)

In `src/App.tsx`, dentro `AppShell`:
- `MsalAuthenticationTemplate` (`InteractionType.Redirect`, `errorComponent={AuthErrorPage}`)
- `AccountsProvider`
- `TransactionsProvider`
- `CategoriesProvider`
- `TableContextProvider`
- `TransactionModalProvider`
- contenuto: `HomeHeader` + `Routes` (routing dettagliato nel modulo 03)

## Motivazione
Centralizza autenticazione, tema e notifiche in alto nell'albero React, prima ancora del routing. I provider di dominio (account, transazioni, categorie, tabella, modale transazione) vengono inizializzati subito dopo l'autenticazione, dentro l'area protetta, così sono disponibili a tutte le pagine.

## File sorgente
- `src/main.tsx`
- `src/App.tsx`
- `src/auth/msalInstance.ts`
- `src/stores/ThemeContext.tsx`
- `src/stores/ApiErrorContext.tsx`
- `src/stores/SuccessMessageContext.tsx`
- `src/stores/AccountContext.tsx`, `TransactionContext.tsx`, `CategoryContext.tsx`, `TableContext.tsx`, `TransactionModalContext.tsx`
