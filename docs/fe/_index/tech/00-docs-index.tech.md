# Frontend Handbook (Tecnico)

## Scopo
Indice tecnico della documentazione modulare FE.

## Come usare questi documenti
1. Parti da `01-overview-and-boundaries`.
2. Segui l'ordine numerico fino a `12-ui-theming-and-design-system`.
3. Per ogni modulo tecnico esiste la versione non tecnica con lo stesso ID.

## Mappa moduli (tech -> non-tech)
- 01 Overview and boundaries
- 02 Bootstrap and providers
- 03 Routing and shell
- 04 Authentication (MSAL)
- 05 API integration and errors
- 06 Data contracts and models
- 07 Global state (contexts)
- 08 Transactions flow
- 09 Categories flow
- 10 Accounts and transfers flow
- 11 Dashboard and time series
- 12 UI theming and design system

## Sorgenti principali
- `src/main.tsx`
- `src/App.tsx`
- `src/config/*`
- `src/auth/*`
- `src/services/*`
- `src/stores/*`
- `src/pages/*`
- `src/components/*`

## Regole di manutenzione
- Quando cambia una feature FE, aggiornare sia file `.tech.md` sia file `.non-tech.md` corrispondente.
- Mantenere stesso ID e stesso titolo logico tra le due versioni.