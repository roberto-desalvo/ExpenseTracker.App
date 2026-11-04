# 03 - Routing and shell (Tecnico)

## Routing
`src/app/app.routes.ts` definisce le rotte con `loadComponent` (ogni pagina è un chunk lazy). Le costanti dei percorsi sono in `src/app/core/config/routes.ts` (`ROUTES`):
- `""` → `DashboardPage` (`features/dashboard/dashboard-page.ts`, modulo 11)
- `movimenti` → `TransactionsPage` (`features/transactions/transactions-page.ts`, modulo 08)
- `analisi` → `AnalyticsPage` (`features/analytics/analytics-page.ts`, modulo 11)
- `impostazioni` → `SettingsPage` (`features/settings/settings-page.ts`)
- `**` → `redirectTo: ""`

Le rotte sono renderizzate dal `router-outlet` di `AppLayout`, che esiste solo a utente autenticato (il gate è nel componente radice, modulo 04).

### Deep link in produzione (Azure Static Web Apps)
Il routing è lato client, quindi l'host deve servire `index.html` per qualsiasi percorso. `web/public/staticwebapp.config.json` (copiato in `dist/web/browser/`) definisce `navigationFallback.rewrite = "/index.html"`, escludendo `/assets/*` e i file statici per estensione. Senza questo file, aprire o ricaricare `/movimenti`, `/analisi` o `/impostazioni` restituisce il 404 di SWA.

### Impostazioni
`SettingsPage` ha tre sezioni selezionate dalla querystring `?sezione=categorie|conti|aspetto` (default `categorie`; accetta anche il vecchio `?tab=categorie|account`, `normalizeSection`). Su desktop (≥ 900px) le sezioni sono una colonna di navigazione verticale; su mobile una `mat-tab-nav-bar` a larghezza piena (link con `routerLink` + `queryParams`). Il contenuto è `CategoriesSettings`, `AccountsSettings` o `AppearanceSettings` (`features/settings/`).

## Shell (`src/app/layout/`)
- `app-layout.ts/.html/.scss` avvolge tutte le route. La variante dipende dalla preferenza `nav` di `ThemeService` (modulo 12) e dal breakpoint `(min-width: 900px)` (`BreakpointObserver`):
  - `sidebar` (default): barra laterale sticky, 256px, comprimibile a 76px (solo icone con tooltip). Stato compresso in `localStorage` (`expense-tracker:sidebar-collapsed`). Contiene logo (`BrandMark`), "Nuovo", voci di menu, switch tema, comprimi/espandi e box utente con logout.
  - `topbar`: header sticky semitrasparente (`backdrop-filter`), logo, voci orizzontali, switch tema, "Nuovo" e `UserMenuButton` (link ad Aspetto e logout).
  - Sotto 900px entrambe diventano una barra compatta (hamburger, logo, switch tema, "+") con un pannello laterale a scomparsa che riusa lo stesso `ng-template` della sidebar; si chiude a ogni navigazione, con clic sullo sfondo o con Esc.
- Voci di menu: `src/app/core/config/navigation.ts` (`NAV_ITEMS` con nome dell'icona Material Symbols; `isNavItemActive` usa il prefisso del percorso).
- `header-actions.ts`: `NewEntryButton` (`mat-menu` "Transazione"/"Trasferimento" → `QuickActionsService`), `ThemeModeButton` (`toggleTheme`), `UserMenuButton`.
- Le pagine usano `PageContainer` (larghezza max 1440px, padding responsive) e `PageHeader` (titolo, sottotitolo, eyebrow; azioni proiettate con l'attributo `actions`) da `src/app/shared/ui/`.

## Note
L'intera area route è protetta: senza utente autenticato `App` mostra la pagina di login invece della shell (modulo 04).
