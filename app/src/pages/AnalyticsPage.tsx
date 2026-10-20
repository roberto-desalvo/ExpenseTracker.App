import { useEffect, useMemo, useState } from "react";
import {
  Alert,
  Box,
  Collapse,
  Skeleton,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { TimeSeriesList } from "../models/TimeSeries";
import TransactionService from "../services/TransactionService";
import { useAccounts } from "../stores/AccountContext";
import { useCategories } from "../stores/CategoryContext";
import { useTransactions } from "../stores/TransactionContext";
import PageContainer from "../components/ui/PageContainer";
import PageHeader from "../components/ui/PageHeader";
import SurfaceCard from "../components/ui/SurfaceCard";
import NetWorthAreaChart from "../components/charts/NetWorthAreaChart";
import CashFlowChart, { CashFlowPoint } from "../components/charts/CashFlowChart";
import BalanceLinesChart, { LineSeries } from "../components/charts/BalanceLinesChart";
import StackedPeriodBars, { StackSeries } from "../components/charts/StackedPeriodBars";
import RankedBarList from "../components/charts/RankedBarList";
import { ChartView, ChartViewToggle } from "../components/charts/ChartParts";
import { buildEntityColorMap } from "../components/charts/entityColors";
import { startOfMonth, toDateInputValue, toIsoDateEnd, toIsoDateStart } from "../utilities/date.utilities";
import {
  enumerateMonthKeys,
  formatCurrency,
  formatPercent,
  formatSignedCurrency,
} from "../utilities/format.utilities";

type RangePreset = "3m" | "6m" | "12m" | "24m" | "ytd" | "custom";
type Granularity = 1 | 2 | 3 | 4;

const PRESETS: { value: RangePreset; label: string }[] = [
  { value: "3m", label: "3 mesi" },
  { value: "6m", label: "6 mesi" },
  { value: "12m", label: "12 mesi" },
  { value: "24m", label: "24 mesi" },
  { value: "ytd", label: "Da inizio anno" },
  { value: "custom", label: "Personalizzato" },
];

const GRANULARITIES: { value: Granularity; label: string }[] = [
  { value: 1, label: "Giorno" },
  { value: 2, label: "Settimana" },
  { value: 3, label: "Mese" },
  { value: 4, label: "Anno" },
];

const rangeFor = (preset: RangePreset): { start: string; end: string } => {
  const today = new Date();
  const end = toDateInputValue(today);
  switch (preset) {
    case "3m":
      return { start: toDateInputValue(startOfMonth(today, -2)), end };
    case "6m":
      return { start: toDateInputValue(startOfMonth(today, -5)), end };
    case "24m":
      return { start: toDateInputValue(startOfMonth(today, -23)), end };
    case "ytd":
      return { start: toDateInputValue(new Date(today.getFullYear(), 0, 1)), end };
    default:
      return { start: toDateInputValue(startOfMonth(today, -11)), end };
  }
};

const dimensionId = (dimensions: { key: string; value: string }[], key: string) => {
  const raw = dimensions.find((d) => d.key === key)?.value;
  return raw === undefined ? NaN : Number(raw);
};

function SummaryFigure({ label, value, hint, tone }: { label: string; value: string; hint?: string; tone?: string }) {
  const a = useTheme().app;
  return (
    <Box sx={{ minWidth: 0 }}>
      <Typography sx={{ fontSize: "0.76rem", fontWeight: 600, color: a.textSecondary }}>{label}</Typography>
      <Typography
        sx={{
          fontFamily: a.headingFontFamily,
          fontSize: "1.3rem",
          fontWeight: 750,
          letterSpacing: "-0.02em",
          color: tone ?? a.text,
          lineHeight: 1.3,
        }}
      >
        {value}
      </Typography>
      {hint && <Typography sx={{ fontSize: "0.72rem", color: a.textMuted }}>{hint}</Typography>}
    </Box>
  );
}

export default function AnalyticsPage() {
  const a = useTheme().app;
  const { accounts, accountsLoaded } = useAccounts();
  const { allCategories, categoriesLoaded } = useCategories();
  const { dataVersion } = useTransactions();

  const [preset, setPreset] = useState<RangePreset>("12m");
  const [range, setRange] = useState(() => rangeFor("12m"));
  const [granularity, setGranularity] = useState<Granularity>(3);
  const [loading, setLoading] = useState(false);

  const [total, setTotal] = useState<TimeSeriesList | null>(null);
  const [byAccount, setByAccount] = useState<TimeSeriesList | null>(null);
  const [flow, setFlow] = useState<TimeSeriesList | null>(null);
  const [byCategory, setByCategory] = useState<TimeSeriesList | null>(null);

  const [flowView, setFlowView] = useState<ChartView>("chart");
  const [accountsView, setAccountsView] = useState<ChartView>("chart");
  const [categoryView, setCategoryView] = useState<ChartView>("chart");
  const [categoryKind, setCategoryKind] = useState<"spent" | "earned">("spent");

  const invalidRange = range.start > range.end;
  // chiavi stabili: un nuovo array con gli stessi id (es. dopo una rinomina) non deve far ripartire le chiamate
  const accountIdsKey = accounts.map((x) => x.id).join(",");
  const categoryIdsKey = allCategories.map((x) => x.id).join(",");
  const accountIds = useMemo(() => accounts.map((x) => x.id), [accountIdsKey]);
  const categoryIds = useMemo(() => allCategories.map((x) => x.id), [categoryIdsKey]);
  // le serie per conto/categoria dipendono dalle anagrafiche: si attende il loro primo caricamento
  const lookupsReady = accountsLoaded && categoriesLoaded;

  const choosePreset = (next: RangePreset) => {
    setPreset(next);
    if (next !== "custom") setRange(rangeFor(next));
  };

  useEffect(() => {
    if (!lookupsReady || invalidRange || !range.start || !range.end) return;

    const base = {
      startDate: toIsoDateStart(range.start),
      endDate: toIsoDateEnd(range.end),
      granularity,
    };

    const load = async () => {
      setLoading(true);
      try {
        const [t, acc, f, cat] = await Promise.all([
          // il saldo (stock) considera sempre i trasferimenti: sono movimenti reali per il singolo conto
          TransactionService.getStock({ ...base, idAccounts: [], idCategories: [] }),
          accountIds.length
            ? TransactionService.getStock({ ...base, idAccounts: accountIds, idCategories: [] })
            : Promise.resolve(null),
          TransactionService.getTimeSeries({ ...base, idAccounts: [], idCategories: [], excludeTransfers: true }),
          categoryIds.length
            ? TransactionService.getTimeSeries({ ...base, idAccounts: [], idCategories: categoryIds, excludeTransfers: true })
            : Promise.resolve(null),
        ]);
        setTotal(t);
        setByAccount(acc);
        setFlow(f);
        setByCategory(cat);
      } catch {
        // errore già mostrato dal layer API
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, [range.start, range.end, granularity, invalidRange, lookupsReady, accountIds, categoryIds, dataVersion]);

  const netWorth = useMemo(
    () => (total?.series[0]?.values ?? []).map((v) => ({ period: v.period, amount: v.amount })),
    [total],
  );

  const cashFlow = useMemo<CashFlowPoint[]>(() => {
    const values = flow?.series[0]?.values ?? [];
    if (granularity !== 3) {
      return values.map((v) => ({ period: v.period, earned: v.earned, spent: v.spent }));
    }
    const byPeriod = new Map(values.map((v) => [v.period, v]));
    return enumerateMonthKeys(new Date(`${range.start}T00:00:00`), new Date(`${range.end}T00:00:00`)).map((period) => ({
      period,
      earned: byPeriod.get(period)?.earned ?? 0,
      spent: byPeriod.get(period)?.spent ?? 0,
    }));
  }, [flow, granularity, range.start, range.end]);

  const accountColors = useMemo(
    () => buildEntityColorMap(accountIds, a.chart.series, a.chart.other),
    [accountIds, a.chart.series, a.chart.other],
  );

  const accountSeries = useMemo<LineSeries[]>(
    () =>
      (byAccount?.series ?? []).map((s, index) => {
        const id = dimensionId(s.dimensions, "AccountId");
        return {
          key: `acc-${Number.isFinite(id) ? id : index}`,
          name: accounts.find((x) => x.id === id)?.name ?? `Conto ${id}`,
          color: accountColors.get(id) ?? a.chart.other,
          values: s.values.map((v) => ({ period: v.period, amount: v.amount })),
        };
      }),
    [byAccount, accounts, accountColors, a.chart.other],
  );

  const categorySeries = useMemo<StackSeries[]>(
    () =>
      (byCategory?.series ?? []).map((s, index) => {
        const id = dimensionId(s.dimensions, "CategoryId");
        return {
          key: `cat-${Number.isFinite(id) ? id : index}`,
          name: allCategories.find((x) => x.id === id)?.name ?? `Categoria ${id}`,
          values: s.values.map((v) => ({ period: v.period, value: categoryKind === "spent" ? v.spent : v.earned })),
        };
      }),
    [byCategory, allCategories, categoryKind],
  );

  const categoryTotals = useMemo(
    () =>
      categorySeries.map((s) => ({
        id: s.key,
        name: s.name,
        value: s.values.reduce((sum, v) => sum + v.value, 0),
      })),
    [categorySeries],
  );

  const summary = useMemo(() => {
    const earned = cashFlow.reduce((s, p) => s + p.earned, 0);
    const spent = cashFlow.reduce((s, p) => s + p.spent, 0);
    const startValue = netWorth[0]?.amount ?? 0;
    const endValue = netWorth[netWorth.length - 1]?.amount ?? 0;
    return {
      earned,
      spent,
      net: earned - spent,
      rate: earned > 0 ? ((earned - spent) / earned) * 100 : null,
      startValue,
      endValue,
      change: endValue - startValue,
    };
  }, [cashFlow, netWorth]);

  const periodLabel = `${new Date(`${range.start}T00:00:00`).toLocaleDateString("it-IT", { dateStyle: "medium" })} – ${new Date(
    `${range.end}T00:00:00`,
  ).toLocaleDateString("it-IT", { dateStyle: "medium" })}`;

  const chartSkeleton = <Skeleton variant="rounded" height={330} />;

  return (
    <PageContainer>
      <PageHeader title="Analisi" subtitle="Come si muovono patrimonio, entrate e spese nel tempo." />

      {/* Filtri: un'unica riga sopra tutti i grafici */}
      <SurfaceCard padding={2}>
        <Stack direction={{ xs: "column", lg: "row" }} spacing={2} alignItems={{ xs: "stretch", lg: "center" }}>
          <Box sx={{ overflowX: "auto", mx: { xs: -0.5, lg: 0 }, px: { xs: 0.5, lg: 0 } }}>
            <ToggleButtonGroup
              exclusive
              size="small"
              value={preset}
              onChange={(_e, value: RangePreset | null) => value && choosePreset(value)}
              aria-label="Periodo"
              sx={{ flexWrap: "nowrap" }}
            >
              {PRESETS.map((p) => (
                <ToggleButton key={p.value} value={p.value} sx={{ whiteSpace: "nowrap" }}>
                  {p.label}
                </ToggleButton>
              ))}
            </ToggleButtonGroup>
          </Box>
          <Box sx={{ flex: 1 }} />
          <Stack direction="row" spacing={1} alignItems="center">
            <Typography sx={{ fontSize: "0.8rem", color: a.textSecondary, whiteSpace: "nowrap" }}>Raggruppa per</Typography>
            <ToggleButtonGroup
              exclusive
              size="small"
              value={granularity}
              onChange={(_e, value: Granularity | null) => value && setGranularity(value)}
              aria-label="Raggruppamento"
            >
              {GRANULARITIES.map((g) => (
                <ToggleButton key={g.value} value={g.value}>
                  {g.label}
                </ToggleButton>
              ))}
            </ToggleButtonGroup>
          </Stack>
        </Stack>
        <Collapse in={preset === "custom"} unmountOnExit>
          <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} sx={{ mt: 2 }}>
            <TextField
              type="date"
              size="small"
              label="Dal"
              value={range.start}
              onChange={(e) => setRange((r) => ({ ...r, start: e.target.value }))}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            <TextField
              type="date"
              size="small"
              label="Al"
              value={range.end}
              onChange={(e) => setRange((r) => ({ ...r, end: e.target.value }))}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Stack>
        </Collapse>
      </SurfaceCard>

      {invalidRange && <Alert severity="warning">La data di inizio deve precedere la data di fine.</Alert>}

      {/* Flusso di cassa + sintesi del periodo */}
      <SurfaceCard
        title="Flusso di cassa"
        subtitle={`Entrate sopra lo zero, uscite sotto, saldo netto come linea · ${periodLabel}`}
        action={<ChartViewToggle value={flowView} onChange={setFlowView} />}
      >
        <Box
          sx={{
            display: "grid",
            gap: 2.5,
            gridTemplateColumns: { xs: "1fr 1fr", md: "repeat(3, 1fr)", xl: "repeat(6, 1fr)" },
            mb: 3,
            pb: 2.5,
            borderBottom: `1px solid ${a.border}`,
          }}
        >
          <SummaryFigure label="Inizio periodo" value={formatCurrency(summary.startValue)} />
          <SummaryFigure label="Fine periodo" value={formatCurrency(summary.endValue)} />
          <SummaryFigure
            label="Variazione"
            value={formatSignedCurrency(summary.change)}
            tone={summary.change >= 0 ? a.income : a.expense}
          />
          <SummaryFigure label="Entrate totali" value={formatCurrency(summary.earned)} hint="trasferimenti esclusi" />
          <SummaryFigure label="Uscite totali" value={formatCurrency(summary.spent)} hint="trasferimenti esclusi" />
          <SummaryFigure
            label="Tasso di risparmio"
            value={summary.rate === null ? "—" : formatPercent(summary.rate)}
            hint={`risparmio netto ${formatSignedCurrency(summary.net)}`}
          />
        </Box>
        {loading ? chartSkeleton : <CashFlowChart points={cashFlow} height={300} view={flowView} />}
      </SurfaceCard>

      <Box sx={{ display: "grid", gap: { xs: 2, md: 2.5 }, gridTemplateColumns: { xs: "1fr", lg: "1fr 1fr" } }}>
        <SurfaceCard title="Patrimonio" subtitle="Saldo medio di tutti i conti per periodo">
          {loading ? chartSkeleton : <NetWorthAreaChart points={netWorth} height={300} />}
        </SurfaceCard>

        <SurfaceCard
          title="Saldo per conto"
          subtitle="Clicca sulla legenda per nascondere un conto"
          action={<ChartViewToggle value={accountsView} onChange={setAccountsView} />}
        >
          {loading ? chartSkeleton : <BalanceLinesChart series={accountSeries} height={300} view={accountsView} />}
        </SurfaceCard>
      </Box>

      <Box sx={{ display: "grid", gap: { xs: 2, md: 2.5 }, gridTemplateColumns: { xs: "1fr", lg: "2fr 1fr" } }}>
        <SurfaceCard
          title={categoryKind === "spent" ? "Spese per categoria" : "Entrate per categoria"}
          subtitle="Composizione per periodo: le categorie minori sono raggruppate in “Altre”"
          action={
            <Stack direction="row" spacing={1}>
              <ToggleButtonGroup
                size="small"
                exclusive
                value={categoryKind}
                onChange={(_e, value: "spent" | "earned" | null) => value && setCategoryKind(value)}
              >
                <ToggleButton value="spent">Uscite</ToggleButton>
                <ToggleButton value="earned">Entrate</ToggleButton>
              </ToggleButtonGroup>
              <ChartViewToggle value={categoryView} onChange={setCategoryView} />
            </Stack>
          }
        >
          {loading ? chartSkeleton : <StackedPeriodBars series={categorySeries} height={300} view={categoryView} />}
        </SurfaceCard>

        <SurfaceCard title="Classifica categorie" subtitle="Totale nel periodo selezionato">
          {loading ? (
            chartSkeleton
          ) : (
            <RankedBarList
              items={categoryTotals}
              maxItems={8}
              color={categoryKind === "spent" ? a.expense : a.income}
              emptyMessage="Nessun movimento nel periodo"
            />
          )}
        </SurfaceCard>
      </Box>
    </PageContainer>
  );
}
