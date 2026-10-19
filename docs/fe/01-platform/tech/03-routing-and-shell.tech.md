# 03 - Routing and shell (Tecnico)

## Routing
`src/App.tsx` usa `BrowserRouter` con un componente interno `AppShell`. I percorsi sono costanti in `src/config/routes.ts` (`ROUTES`):
- `/` → `DashboardPage` (panoramica del mese e del patrimonio, modulo 11)
- `/movimenti` → `TransactionsPage` (tabella transazioni con filtri, modulo 08)
- `/analisi` → `AnalyticsPage` (serie temporali con periodo e granularità, modulo 11)
- `/impostazioni` → `SettingsPage`
- `*` → `<Navigate to="/" replace />`

`DATA_ROUTES` (stesso file) elenca le rotte su cui `AccountsProvider`/`CategoriesProvider` caricano l'anagrafica (oggi tutte).

### Impostazioni
`SettingsPage` (`src/pages/SettingsPage.tsx`) ha tre sezioni selezionate dalla querystring `?sezione=categorie|conti|aspetto` (default `categorie`; per compatibilità accetta anche il vecchio `?tab=categorie|account`, `normalizeSection`). Su desktop (`md+`) le sezioni sono una colonna di navigazione verticale a sinistra; su mobile sono `Tabs` a larghezza piena. Il contenuto è `CategoriesPage`, `AccountsPage` o `AppearanceSettings` (`src/components/settings/AppearanceSettings.tsx`).

## Shell
- `src/components/layout/AppLayout.tsx` avvolge tutte le route. La variante dipende dalla preferenza `nav` di `ThemeContext` (modulo 12):
  - `sidebar` (default): barra laterale sticky a sinistra, larghezza 256px, comprimibile a 76px (solo icone con tooltip). Lo stato compresso è in `localStorage` (`expense-tracker:sidebar-collapsed`). Contiene logo (`BrandMark`), bottone "Nuovo", voci di menu, switch tema, comprimi/espandi e box utente con logout.
  - `topbar`: header sticky con sfondo semitrasparente (`backdrop-filter`), logo, voci di menu orizzontali, switch tema, "Nuovo" e menu utente (`UserMenuButton`, con link ad Aspetto e logout).
  - Sotto `md` entrambe le varianti diventano una barra superiore compatta (menu hamburger, logo, switch tema, "+") con `Drawer` laterale che riusa il contenuto della sidebar.
- Voci di menu: `src/components/layout/navigation.tsx` (`NAV_ITEMS`: Dashboard, Movimenti, Analisi, Impostazioni; `isNavItemActive` usa il prefisso del percorso).
- `src/components/layout/HeaderActions.tsx`: `NewEntryButton` (menu "Transazione"/"Trasferimento" → `useQuickActions()`), `ThemeModeButton` (`toggleTheme`), `UserMenuButton`.
- A ogni cambio di rotta il drawer mobile si chiude e la pagina torna in cima.
- Le pagine usano `PageContainer` (larghezza max 1440px, padding responsive) e `PageHeader` (titolo, sottotitolo, azioni) da `src/components/ui/`.

## Note
L'intera area route è protetta da `AuthGate` (MSAL o login di sviluppo; vedi modulo 04).
