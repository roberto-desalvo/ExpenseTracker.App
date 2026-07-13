# 03 - Routing and shell (Tecnico)

## Routing
`src/App.tsx` usa `BrowserRouter` con route:
- `/` -> `LandingPage`
- `/transazioni` -> `TransactionsPage`
- `/categorie` -> `CategoriesPage`
- `/account` -> `AccountsPage`

## Shell
- Header condiviso: `src/components/HomeHeader.tsx`
- Contenitore pagina: `Box` MUI full-height.

## Note
L'intera area route è protetta dal template di autenticazione MSAL.
