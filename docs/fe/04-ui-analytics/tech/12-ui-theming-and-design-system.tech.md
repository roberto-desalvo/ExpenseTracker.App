# 12 - UI theming and design system (Tecnico)

## Stili (preset)
`src/theme/stylePresets.ts` definisce tre stili (`StylePresetId`), ciascuno con token per modalità chiara e scura (`ModeTokens`):
| Preset | Carattere | Font | Raggio card/controlli |
|---|---|---|---|
| `aurora` (default) | indaco, card sospese con ombra morbida, hero a gradiente | Inter | 18 / 10 px |
| `ledger` | toni caldi, verde petrolio, bordi senza ombra | Manrope | 12 / 8 px |
| `graphite` | monocromatico, accento lime nella hero, angoli vivi | IBM Plex Sans | 6 / 4 px |

`ModeTokens`: `background`, `surface`, `surfaceAlt`, `border`, `borderStrong`, `text`, `textSecondary`, `textMuted`, `primary`/`primaryHover`/`primaryContrast`/`primarySoft`, `income`/`expense` (+ varianti `Soft`), `cardShadow`, `sidebarBackground`, `hero` (`background`, `text`, `subtle`, `spark`, `border`). I font sono caricati da Google Fonts in `index.html`.

## Logo e favicon
Il logo (tre barre crescenti) è disegnato in `src/components/layout/BrandMark.tsx` con i token `hero` del preset attivo. L'icona della scheda del browser è `public/favicon.svg` (referenziata in `index.html`): stesso disegno con i colori fissi della hero di `aurora` chiaro, perché il favicon è statico e non segue il preset scelto.

## Tema MUI
- `src/theme/themes.ts` → `buildAppTheme(presetId, mode)`. Estende il tema MUI con `theme.app: AppTokens` (token del preset + `presetId`, `mode`, raggi, font titoli e `chart`: `series`, `other`, `grid`, `axis`, `net`). I componenti leggono i token con `useTheme().app`.
- Mantiene per compatibilità `theme.palette.custom` (stesse chiavi di prima, ora mappate sui token).
- Override globali: `MuiButton` (no maiuscolo, no elevazione), `MuiOutlinedInput`, `MuiMenu`/`MuiMenuItem`, `MuiDialog`, `MuiBackdrop` (blur), `MuiTooltip` (contrasto invertito), `MuiChip`, `MuiTableCell`, `MuiToggleButton(Group)` (stile segmented control), `MuiTab`, scrollbar.

## Preferenze di aspetto
`src/stores/ThemeContext.tsx` (`AppThemeProvider`, `useAppTheme`):
- `preferences: { preset, mode: "light"|"dark"|"system", nav: "sidebar"|"topbar" }`, persistite in `localStorage` (`expense-tracker:appearance`, letture/scritture in `try/catch`; default `aurora` / `system` / `sidebar`).
- `mode` effettivo risolve `system` con `useMediaQuery("(prefers-color-scheme: dark)")`.
- Azioni: `toggleTheme()` (passa a chiaro/scuro esplicito), `setPreset`, `setModePreference`, `setNavLayout`.
- UI: `ThemeModeButton` nella shell; pagina **Impostazioni → Aspetto** (`AppearanceSettings.tsx`) con card selezionabili e anteprime dal vivo di ogni stile (mini dashboard disegnata con i token del preset nella modalità corrente), tema (Chiaro/Scuro/Automatico) e navigazione (Barra laterale/Barra superiore).

## Palette dei grafici
- `src/theme/chartColors.ts`: `CHART_SERIES_COLORS_LIGHT` / `_DARK`, 8 colori categoriali in ordine fisso, validati per daltonismo (ΔE CVD ≥ 8 tra slot adiacenti) sulle superfici chiare e scure di tutti i preset; `getSeriesColors(mode)`. In chiaro 3 slot sono sotto 3:1 di contrasto: per questo ogni grafico multi-serie ha legenda, tooltip e vista tabella.
- Gli slot si assegnano per entità con `buildEntityColorMap` (mai ciclando); oltre l'ottava serie si usa `chart.other` (grigio).
- Entrate/uscite usano i token semantici `income`/`expense` del preset, non la palette categoriale.

## Componenti UI condivisi (`src/components/ui/`)
`PageContainer`, `PageHeader`, `SurfaceCard` (superficie con titolo/sottotitolo/azione), `EmptyState`, `SearchField` (debounce), `EditorDrawer` (pannello laterale di modifica). `AppModal` e `ConfirmDeleteDialog` (`src/components/`) sono `Dialog` MUI con intestazione, azioni nel footer e schermo intero su mobile (`AppModal`).

## Tabelle
`src/components/DataTableBase.tsx`: card con bordo e raggio del preset, intestazione sticky su sfondo `surfaceAlt`, righe a filo con separatori sottili, skeleton durante il caricamento, `EmptyState` con azione opzionale (`emptyAction`), `TablePagination` in italiano. Le azioni di riga sono icone inline visibili all'hover/focus (sempre visibili su mobile).

## Stile globale
`src/index.css`: direttive Tailwind, keyframe `fadeUp` (ingresso card) e `slideDown`, riduzione animazioni con `prefers-reduced-motion`. Formattazione numeri e date in `src/utilities/format.utilities.ts` (`formatCurrency` con separatore delle migliaia sempre attivo, `formatSignedCurrency`, `formatCompact`, `formatPercent`, `formatPeriodLabel`).
- Utility: Tailwind v3 (`tailwind.config.cjs`, wrapped by `withMT` from Material Tailwind; v4 is not supported by `@material-tailwind/react` v2).

## Obiettivo
Mantenere coerenza visiva tra pagine, tabelle, filtri e grafici, lasciando all'utente la scelta della direzione estetica.
