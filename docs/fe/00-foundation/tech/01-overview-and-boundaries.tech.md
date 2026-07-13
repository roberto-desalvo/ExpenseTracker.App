# 01 - Overview and boundaries (Tecnico)

## Obiettivo
Descrivere stack, responsabilità e confini del FE.

## Stack
- React 18 + TypeScript + Vite
- UI: MUI, Tailwind, Recharts
- Auth: MSAL (`@azure/msal-browser`, `@azure/msal-react`)

## Confine FE
- Rendering UI, orchestrazione chiamate API, stato client e filtri.
- Non include regole di dominio persistenti (delegate al backend).

## Entry points
- `src/main.tsx`
- `src/App.tsx`

## Dipendenze esterne
- Runtime env da `.env.development` / `.env.example`
- API backend via `VITE_EXPENSE_TRACKER_API_BASE_URL`
