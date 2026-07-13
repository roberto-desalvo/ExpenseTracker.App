# 12 - UI theming and design system (Tecnico)

## Theming
- `src/theme/themes.ts` definisce `lightTheme` e `darkTheme` (via `createTheme` di MUI) con palette `custom` estesa (dichiarazione di modulo `@mui/material/styles`): `appBackground`, `drawerBackground`/`drawerBorder`, `filterBorder`/`filterBackground`/`filterText`, `accentColor`/`accentHover`, `amountPositive`/`amountNegative`, `badgeBackground`/`badgeBorder`/`badgeText`, `tableHeaderText`, `rowBackground`/`rowHover`, `actionButtonColor`/`actionButtonBorder`/`actionButtonBackground`/`actionButtonHover`.
- `src/stores/ThemeContext.tsx` (`AppThemeProvider`) espone `mode` (`"light" | "dark"`) e `toggleTheme()` tramite il context `useAppTheme`, e avvolge l'app in `MuiThemeProvider` + `CssBaseline`. Lo stato iniziale è sempre `mode = "light"` (nessuna persistenza né rilevamento della preferenza di sistema).
- **Stato attuale**: `useAppTheme`/`toggleTheme` non sono richiamati da nessun componente della UI (né `HomeHeader.tsx` né altre pagine espongono un controllo di switch tema); il tema scuro (`darkTheme`) è definito e pronto all'uso ma non è raggiungibile dall'utente finale al momento — l'app resta fissa in modalità chiara.

## Palette colori grafici
- `src/theme/chartColors.ts` esporta `CHART_SERIES_COLORS`, un array fisso di 12 colori esadecimali; è l'unica fonte di colori per le serie multiple nei grafici (`TimeSeriesLineChart`, `MonthlyDistributionPieChart` e i legacy `AccountsPieChart`/`CategoriesPieChart`), assegnati ciclicamente per indice (`index % CHART_SERIES_COLORS.length`).
- I colori semantici di importo (verde/rosso per positivo/negativo) non vengono da `chartColors.ts` ma dai token `custom.amountPositive` / `custom.amountNegative` di `themes.ts`.

## Stile
- Base globale: `src/index.css` (direttive Tailwind, font `Inter` di default, keyframe `slideDown` usato per l'animazione di espansione delle sezioni della dashboard), `src/App.css` (residuo del template Vite, in gran parte non utilizzato: stile `.logo`/`.card`/`.read-the-docs`).
- Utility: Tailwind (`tailwind.config.js`)
- Componenti: MUI + componenti FE custom in `src/components/*`

## Tabelle
- Componente generico condiviso: `src/components/DataTableBase.tsx`, usato ad esempio da `ExpenseTable.tsx`. Convenzioni:
  - Contenitore `Paper` con bordo arrotondato (`borderRadius: 4`) e sfondo trasparente.
  - Intestazione (`TableHead`) sticky (`position: sticky, top: 0`) quando è impostata un'altezza viewport (`tableViewportHeight`), testo in maiuscolo con colore `custom.tableHeaderText`.
  - Righe con `borderSpacing` verticale (tabella "a schede" separate, non griglia continua classica).
  - Overlay di caricamento semi-trasparente con `CircularProgress` sopra il contenuto esistente (mantiene il layout stabile durante il refresh).
  - Stato vuoto centrato con messaggio principale + sottotesto opzionale (`emptyMessage`/`emptySubtext`).
  - Paginazione tramite `TablePagination` con `rowsPerPageOptions` configurabili per singolo utilizzo.

## Obiettivo
Mantenere coerenza visiva tra pagine, tabelle, filtri e grafici.
