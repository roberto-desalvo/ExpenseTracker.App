# 11 - Dashboard and time series (Tecnico)

## Home dashboard
- Pagina: `src/pages/LandingPage.tsx`
- Dati: `TransactionService.getLanding({ excludeTransfers: true })` → modello `LandingDashboard` (`src/models/LandingDashboard.ts`): `asOf`, `monthStart`, `accounts: LandingAccountBalance[]`, `categories: LandingCategorySummary[]`, `totals: LandingTotals`, `netWorthSeries: TimeSeriesList`.
- La pagina è divisa in due sezioni comprimibili (stato persistito in `localStorage`, chiavi `expense-tracker:home-questo-mese-expanded` e `expense-tracker:home-patrimonio-expanded`), oltre allo stato di visibilità del saldo (`expense-tracker:home-balance-visible`).

### Sezione "Questo mese"
- Card riepilogo (`SummaryCard`, componente locale in `LandingPage.tsx`): Saldo totale (con toggle mostra/nascondi tramite icona occhio), Entrate mese, Uscite mese, Bilancio mese; colori presi da `theme.palette.custom.amountPositive` / `amountNegative`.
- Blocco "Transazioni mese corrente": `TransactionsSummaryBar` (chip entrate/uscite/bilancio, renderizzato con `showChips={false}` in questa pagina quindi non visibile qui), `AccountsBar` (filtri account/categoria/mese + azioni Aggiungi/Aggiorna) ed `ExpenseTable` (tabella transazioni basata su `DataTableBase`).
- Due riquadri "Account" e "Categorie", ciascuno con due grafici a ciambella `MonthlyDistributionPieChart` (Entrate / Uscite) alimentati da `accountIncomeItems`/`accountOutcomeItems`/`categoryIncomeItems`/`categoryOutcomeItems` (derivati da `earnedMonth`/`spentMonth` di `data.accounts` e `data.categories`).

### Sezione "Patrimonio"
- Filtri: data inizio, data fine, granularità (`Select` con valori 1=Giornaliero, 2=Settimanale, 3=Mensile, 4=Annuale, passati come `granularity` nella `TimeSeriesRequest`).
- Grafico "Andamento patrimonio {start} - {end}": `TimeSeriesLineChart` con `tightYAxis`, singola serie "Patrimonio totale" costruita dal primo elemento di `data.netWorthSeries.series`.
- Grafico "Account": `TimeSeriesLineChart` con `enableLegendToggle`, alimentato da `TransactionService.getStock(...)` (una richiesta senza filtri per il totale e una per account); se ci sono più account viene aggiunta una serie "Totale" calcolata sommando i valori per periodo.
- Grafico "Categorie": `TimeSeriesLineChart`, alimentato da `TransactionService.getTimeSeries(...)` filtrato per `idCategories`.
- Il ricaricamento (`handlePatrimonioDashboardLoad`) parte al variare di data inizio/fine/granularità o all'espansione della sezione.

> Nota: esistono ancora nel codice i componenti `src/components/AccountsPieChart.tsx` e `src/components/CategoriesPieChart.tsx` (torte a saldo/entrate-uscite per account/categoria), ma non sono più importati da `LandingPage.tsx`: sono stati sostituiti da `MonthlyDistributionPieChart`. Sono da considerare codice legacy non attualmente collegato alla UI.

## Serie temporali
- Component grafico a linee: `src/components/TimeSeriesLineChart.tsx` (basato su Recharts `LineChart`). Props: `series: TimeSeriesLineChartSeries[]` (`{ name, values: { period, amount }[] }`), `height`, `emptyMessage`, `enableLegendToggle` (default `true`, permette di nascondere/mostrare singole serie cliccando sulla legenda), `tightYAxis` (restringe il dominio dell'asse Y intorno ai valori invece di partire da zero).
- API: `getTimeSeries()` e `getStock()` in `TransactionService`, entrambe accettano `TimeSeriesRequest` (`src/models/TimeSeriesRequest.ts`): `{ startDate, endDate, idAccounts, idCategories, granularity, excludeTransfers? }` e restituiscono `TimeSeriesList` (`src/models/TimeSeries.ts`): `{ granularity, series: { dimensions: { key, value }[], values: { period, amount }[] }[] }`.
- Le serie multiple condividono la stessa palette colori: `CHART_SERIES_COLORS` in `src/theme/chartColors.ts`, assegnata ciclicamente per indice.

## Ambiti analitici attuali
- Landing aggregata (sezioni "Questo mese" e "Patrimonio")
- Analisi in Accounts/Categories tramite `AccountsPieChart`/`CategoriesPieChart` legacy non più montati in Landing (vedi nota sopra)
