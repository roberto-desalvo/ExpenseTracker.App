# Expense Tracker — frontend (Angular)

SPA Angular 22 (componenti standalone, signals, zoneless) con Angular Material ed ECharts.
Documentazione completa in [`docs/fe/`](../docs/fe/_index/tech/00-docs-index.tech.md).

## Configurazione
Copiare `.env.example` in `.env.local` e impostare almeno `VITE_EXPENSE_TRACKER_API_BASE_URL`.
Con `npm start` l'accesso Entra ID è sostituito da un login con il solo nome utente
(il BE deve avere `DevAuth__Enabled=true`); `VITE_DEV_AUTH_BYPASS=false` attiva MSAL anche in locale.

Le variabili vengono scritte in `src/environments/env.generated.ts` (non versionato) da
`scripts/write-env.mjs`, eseguito in automatico prima di `start`, `build`, `test` e `lint`.
L'ambiente di processo ha la precedenza sui file `.env*` (così la CI usa i propri secret).

## Comandi
```bash
npm install
npm start        # dev server su http://localhost:5173
npm run build    # build di produzione in dist/web/browser
npm run lint     # angular-eslint
npm test         # unit test (Vitest)
```

## Struttura (`src/app/`)
- `core/` — config ed env, auth (MSAL, demo, sviluppo), client HTTP e interceptor, store a signal, tema, modelli, utility
- `layout/` — shell (barra laterale/superiore, menu mobile) e azioni dell'header
- `features/` — pagine: dashboard, transactions, analytics, settings
- `pages/auth/` — login Entra ID/demo e login di sviluppo
- `shared/ui/`, `shared/charts/` — componenti riusabili e grafici ECharts
