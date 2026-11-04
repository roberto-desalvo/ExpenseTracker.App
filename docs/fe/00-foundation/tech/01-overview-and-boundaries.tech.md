# 01 - Overview and boundaries (Tecnico)

## Obiettivo
Descrivere stack, responsabilità e confini del FE.

## Stack
- Angular 22 (CLI, builder `@angular/build:application` su esbuild/Vite), TypeScript 6, componenti standalone, **signals**, change detection zoneless, control flow `@if/@for/@switch`.
- UI: Angular Material + CDK (Material 3, rifinito dai token dello stile attivo), icone Material Symbols Rounded (Google Fonts), SCSS (niente Tailwind).
- Grafici: ECharts tramite `ngx-echarts` (import modulare, caricato in lazy).
- Date: `date-fns` + `@angular/material-date-fns-adapter` (locale `it`).
- Auth: `@azure/msal-browser` v5 usato direttamente (senza `msal-angular`), vedi modulo 04.
- Qualità: `angular-eslint` (`npm run lint`), unit test con Vitest (`npm test`).

## Confine FE
- Rendering UI, orchestrazione chiamate API, stato client e filtri.
- Non include regole di dominio persistenti (delegate al backend).

## Entry points
- `src/main.ts` — `bootstrapApplication(App, appConfig)`.
- `src/app/app.config.ts` — provider globali (router, HttpClient con interceptor, inizializzazione auth, date adapter, locale, ECharts), modulo 02.
- `src/app/app.ts` — componente radice: applica il tema e mostra la shell solo a utente autenticato (altrimenti la pagina di login).

## Route di primo livello
Definite in `src/app/app.routes.ts` (componenti caricati in lazy); costanti in `src/app/core/config/routes.ts` (`ROUTES`):
- `/` → `DashboardPage` (panoramica)
- `/movimenti` → `TransactionsPage` (elenco transazioni con filtri)
- `/analisi` → `AnalyticsPage` (andamenti nel tempo)
- `/impostazioni` → `SettingsPage` (sezioni Categorie, Conti, Aspetto)
- qualunque altro percorso → redirect a `/`

## Dipendenze esterne
- Variabili d'ambiente (stessi nomi `VITE_*` del vecchio frontend, così i secret di CI restano invariati), lette da `scripts/write-env.mjs` e validate in `src/app/core/config/env.ts`:
  - `VITE_EXPENSE_TRACKER_API_BASE_URL`
  - `VITE_MSAL_CLIENT_ID`
  - `VITE_MSAL_TENANT_ID`
  - `VITE_MSAL_API_SCOPE`
  - opzionali: `VITE_DEV_AUTH_BYPASS` (`false` per usare MSAL anche con `npm start`), `VITE_DEMO_LOGIN_ENABLED` (`false` per nascondere la demo), vedi modulo 04
- Valori locali in `web/.env.local` (gitignored), template in `web/.env.example`.
- `write-env.mjs` gira come `prestart`/`prebuild`/`pretest`/`prelint`: unisce `.env`, `.env.local`, `.env.<mode>`, `.env.<mode>.local` e l'ambiente di processo (che ha la precedenza, es. secret della CI) e genera `src/environments/env.generated.ts` (gitignored) con `DEV` = `true` solo per `npm start`/test/lint.
- API backend via `VITE_EXPENSE_TRACKER_API_BASE_URL`. Il dev server gira su `http://localhost:5173` (porta impostata in `angular.json`), la stessa del vecchio Vite: CORS del BE e redirect URI di Entra ID non cambiano.

## Comandi (da `web/`)
`npm install`, `npm start` (dev server), `npm run build` (output in `dist/web/browser`), `npm run lint`, `npm test`.
