# 01 - Overview and boundaries (Tecnico)

## Obiettivo
Descrivere stack, responsabilità e confini del FE.

## Stack
- React 18 + TypeScript + Vite (plugin `@vitejs/plugin-react-swc`)
- Routing: React Router v7 (`react-router-dom`)
- UI: MUI v6, Tailwind CSS, Recharts (grafici)
- Auth: MSAL (`@azure/msal-browser`, `@azure/msal-react`)

## Confine FE
- Rendering UI, orchestrazione chiamate API, stato client e filtri.
- Non include regole di dominio persistenti (delegate al backend).

## Entry points
- `src/main.tsx` — inizializza MSAL (salvo bypass di sviluppo) e monta l'app dentro i provider globali.
- `src/App.tsx` — definisce `BrowserRouter` e l'`AppShell` (autenticazione, provider di dominio, routing).

## Route di primo livello
Definite in `AppShell` (`src/App.tsx`), dettagli di routing/shell nel modulo 03:
- `/` → `LandingPage` (dashboard)
- `/impostazioni` → `SettingsPage` (contenitore a tab che incorpora Categorie e Account)

## Dipendenze esterne
- Variabili d'ambiente richieste (validate in `src/config/env.ts`, tipizzate in `src/vite-env.d.ts`):
  - `VITE_EXPENSE_TRACKER_API_BASE_URL`
  - `VITE_MSAL_CLIENT_ID`
  - `VITE_MSAL_TENANT_ID`
  - `VITE_MSAL_API_SCOPE`
  - (le tre MSAL non servono con il bypass di sviluppo; `VITE_DEV_AUTH_BYPASS=false` opzionale per disattivarlo, vedi modulo 04)
- Valori locali in `.env.local` (gitignored), template in `.env.example`
- API backend via `VITE_EXPENSE_TRACKER_API_BASE_URL`
