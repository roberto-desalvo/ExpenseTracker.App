# 03 - Routing and shell (Tecnico)

## Routing
`src/App.tsx` usa `BrowserRouter` con un componente interno `AppShell` che definisce solo due route:
- `/` -> `LandingPage`
- `/impostazioni` -> `SettingsPage`

Non esistono più route dedicate a transazioni/categorie/account. `SettingsPage` (`src/pages/SettingsPage.tsx`) è un contenitore a schede (MUI `Tabs`) che renderizza `CategoriesPage` e `AccountsPage` come tab embedded, passando la prop `embedded` a entrambe. La scheda attiva è derivata dalla querystring `?tab=categorie|account` (default `categorie`, tramite `normalizeTab`); il cambio scheda chiama `navigate("/impostazioni?tab=...")` invece di introdurre nuove route.

## Shell
- Header condiviso: `src/components/HomeHeader.tsx`. È un menu a drawer (icona hamburger `MenuIcon`) con voci "Dashboard" (`/`) e "Impostazioni" (`/impostazioni`), quest'ultima espandibile con le sotto-voci "Categorie" (`/impostazioni?tab=categorie`) e "Account" (`/impostazioni?tab=account`); mostra il nome dell'account MSAL attivo e un pulsante "Esci" (`instance.logoutRedirect()`).
- L'header è sticky (`position: sticky`, prop `sticky`) solo nella route `/` (calcolato in `AppShell` come `isHomeRoute = location.pathname === "/"`); nelle altre route ha `position: relative`.
- Contenitore pagina: `Box` MUI full-height (`minHeight: 100vh`, `display: flex`, `flexDirection: column`) dentro `AppShell`.
- `AppShell` avvolge le route con i provider di dominio `AccountsProvider`, `TransactionsProvider`, `CategoriesProvider`, `TableContextProvider`, `TransactionModalProvider` (distinti dai provider globali di bootstrap in `main.tsx`, vedi modulo 02).

## Note
L'intera area route è protetta dal template di autenticazione MSAL (`MsalAuthenticationTemplate`, redirect flow, fallback `AuthErrorPage`; vedi modulo 04).
