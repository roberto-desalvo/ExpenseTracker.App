# 12 - UI theming and design system (Tecnico)

## Stili (preset)
`src/app/core/theme/stylePresets.ts` definisce tre stili (`StylePresetId`), ciascuno con token per modalità chiara e scura (`ModeTokens`):
| Preset | Carattere | Font | Raggio card/controlli |
|---|---|---|---|
| `aurora` (default) | indaco, card sospese con ombra morbida, hero a gradiente | Inter | 18 / 10 px |
| `ledger` | toni caldi, verde petrolio, bordi senza ombra | Manrope | 12 / 8 px |
| `graphite` | monocromatico, accento lime nella hero, angoli vivi | IBM Plex Sans | 6 / 4 px |

`ModeTokens`: `background`, `surface`, `surfaceAlt`, `border`, `borderStrong`, `text`, `textSecondary`, `textMuted`, `primary`/`primaryHover`/`primaryContrast`/`primarySoft`, `income`/`expense` (+ varianti `Soft`), `cardShadow`, `sidebarBackground`, `hero` (`background`, `text`, `subtle`, `spark`, `border`). I font e le icone (Material Symbols Rounded) sono caricati da Google Fonts in `src/index.html`.

## Logo e favicon
Il logo (tre barre crescenti) è disegnato in `src/app/shared/ui/brand-mark.ts` con le variabili `--app-hero-*` dello stile attivo. L'icona della scheda è `public/favicon.svg`: stesso disegno con i colori fissi della hero di `aurora` chiaro (il favicon è statico).

## Tema: token come variabili CSS
- `src/app/core/theme/theme.service.ts` (`ThemeService`) calcola `tokens()` = `buildAppTokens(preset, mode)` (`AppTokens`: token del preset + `presetId`, `mode`, raggi, font e `chart`: `series`, `other`, `grid`, `axis`, `net`) e li scrive con un `effect()` come **variabili CSS su `:root`**:
  - `--app-*` (es. `--app-surface`, `--app-primary`, `--app-radius-card`, `--app-hero-background`, `--app-font-family`, più derivati come `--app-hover`, `--app-popup-shadow`, `--app-backdrop`, `--app-tooltip-background`);
  - `--mat-sys-*` (colori, superfici, outline e raggi del sistema Material 3), così i componenti Angular Material seguono lo stile scelto;
  - `color-scheme` e gli attributi `data-mode`/`data-preset` su `<html>`.
- Componenti e stili leggono i token come `var(--app-…)`; dove serve un valore in TypeScript (opzioni dei grafici, colori per entità) si usa `ThemeService.tokens()`.
- `src/styles.scss`: base `mat.theme(...)` (theme-type `color-scheme`, tipografia sui font del preset, densità −2) e override con i mixin `mat.*-overrides` (bottoni, form field, menu, select, autocomplete, dialog, tooltip a contrasto invertito, chip, button toggle, datepicker, checkbox); pannelli a discesa con bordo e ombra comuni, voci di menu arrotondate; backdrop con blur; segmented control (`.mat-button-toggle-group.segmented`, equivalente dei vecchi `ToggleButtonGroup`); snackbar (`.app-snack--error`/`--success`); drawer laterale (`.app-drawer`); tabella dati (`.data-table`); utility (`.row`, `.col`, `.spacer`, `.gap-*`, `.nowrap`, `.tabular`, `.overline`, `.skeleton`, `.initials`, `.alert-*`, `.surface`); keyframe `fadeUp`/`slideDown`; riduzione animazioni con `prefers-reduced-motion`; scrollbar.

## Preferenze di aspetto
`ThemeService`:
- `preferences: { preset, mode: "light"|"dark"|"system", nav: "sidebar"|"topbar" }` (signal), persistite in `localStorage` (`expense-tracker:appearance`, letture/scritture in `try/catch`; valori non validi scartati; default `aurora` / `system` / `sidebar`).
- `mode()` effettivo risolve `system` con `matchMedia("(prefers-color-scheme: dark)")` (aggiornato al cambio di impostazione del sistema).
- Azioni: `toggleTheme()` (passa a chiaro/scuro esplicito), `setPreset`, `setModePreference`, `setNavLayout`.
- UI: `ThemeModeButton` nella shell; **Impostazioni → Aspetto** (`features/settings/appearance-settings.*`) con card selezionabili e anteprime dal vivo di ogni stile (mini dashboard disegnata con i token del preset nella modalità corrente), tema (Chiaro/Scuro/Automatico) e navigazione (Barra laterale/Barra superiore).

## Palette dei grafici
- `src/app/core/theme/chartColors.ts`: `CHART_SERIES_COLORS_LIGHT` / `_DARK`, 8 colori categoriali in ordine fisso, validati per daltonismo (ΔE CVD ≥ 8 tra slot adiacenti) sulle superfici chiare e scure di tutti i preset; `getSeriesColors(mode)`. In chiaro 3 slot sono sotto 3:1 di contrasto: per questo ogni grafico multi-serie ha legenda, tooltip e vista tabella.
- Gli slot si assegnano per entità con `buildEntityColorMap` (mai ciclando); oltre l'ottava serie si usa `chart.other` (grigio).
- Entrate/uscite usano i token semantici `income`/`expense` del preset, non la palette categoriale.

## Componenti UI condivisi (`src/app/shared/ui/`)
`PageContainer`, `PageHeader` (azioni con attributo `actions`), `SurfaceCard` (superficie con titolo/sottotitolo; azione a destra con attributo `card-action` e input `hasAction`), `EmptyState`, `SearchField` (debounce, evento `searchChange`), `Pagination`, `EntityAutocomplete` (scelta di conto/categoria con ricerca). Pannelli controllati da un input `open` ed evento `closed`, costruiti su `MatDialog` a partire da un template (`TemplateDialogHost`): `AppModal` (form con intestazione, campi proiettati e azioni; `submitAction` può restituire un messaggio mostrato come notifica di successo; a schermo intero su mobile), `EditorDrawer` (pannello laterale destro di 440px; azione secondaria con attributo `secondary-action`), `ConfirmDeleteDialog` (`confirmAction`). Mentre `busy` è true i pannelli non si chiudono.

## Tabelle
`src/app/shared/ui/data-table.ts` (`DataTable`): card con bordo e raggio del preset, intestazione sticky su sfondo `surfaceAlt`, righe a filo con separatori sottili, skeleton durante il caricamento, `EmptyState` con azione opzionale (attributo `empty-action`), `mat-paginator` in italiano (`ItalianPaginatorIntl`). Le celle di ogni riga arrivano da un `<ng-template let-row>` proiettato. Ordinamento opzionale (`sortable`, `sortBy`, `sortDirection`, evento `sortChange`; lo stato resta al chiamante). `tableLayout` `"auto"` o `"fixed"` (rispetta le larghezze e manda a capo invece di allargare). Le azioni di riga (`.row-actions`) sono icone visibili all'hover/focus su desktop, sempre visibili su mobile.

## Formattazione
`src/app/core/utils/format.utilities.ts` (`formatCurrency` con separatore delle migliaia sempre attivo, `formatSignedCurrency`, `formatCompact`, `formatPercent`, `formatPeriodLabel`, `formatMonthYear`, `initialsOf`) e `date.utilities.ts`.

## Obiettivo
Mantenere coerenza visiva tra pagine, tabelle, filtri e grafici, lasciando all'utente la scelta della direzione estetica.
