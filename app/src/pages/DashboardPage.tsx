import { useEffect, useMemo, useState } from "react";
import { Alert, Box, Button, Skeleton, ToggleButton, ToggleButtonGroup, Tooltip } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import {
  ArrowForwardRounded,
  BarChartRounded,
  DonutLargeRounded,
  InsightsRounded,
  SavingsOutlined,
  SouthWestRounded,
  NorthEastRounded,
  PercentRounded,
} from "@mui/icons-material";
import { useNavigate } from "react-router-dom";
import { LandingDashboard } from "../models/LandingDashboard";
import Transaction from "../models/Transaction";
import TransactionService from "../services/TransactionService";
import { useTransactions } from "../stores/TransactionContext";
import { useTransactionModal } from "../stores/TransactionModalContext";
import { useAuth } from "../auth/AuthContext";
import PageContainer from "../components/ui/PageContainer";
import PageHeader from "../components/ui/PageHeader";
import SurfaceCard from "../components/ui/SurfaceCard";
import HeroBalanceCard from "../components/dashboard/HeroBalanceCard";
import StatTile from "../components/dashboard/StatTile";
import AccountsOverview from "../components/dashboard/AccountsOverview";
import RecentTransactions from "../components/dashboard/RecentTransactions";
import type { CashFlowPoint } from "../components/charts/CashFlowChart";
import RankedBarList from "../components/charts/RankedBarList";
import CategoryDonutChart from "../components/charts/CategoryDonutChart";
import {
  enumerateMonthKeys,
  formatCurrency,
  formatMonthYear,
  formatPercent,
  formatSignedCurrency,
  toMonthKey,
} from "../utilities/format.utilities";
import { startOfMonth } from "../utilities/date.utilities";
import { ROUTES } from "../config/routes";

const BALANCE_VISIBILITY_STORAGE_KEY = "expense-tracker:home-balance-visible";

type BreakdownView = "bars" | "pie";

const readBalanceVisible = () => {
  try {
    return window.localStorage.getItem(BALANCE_VISIBILITY_STORAGE_KEY) === "true";
  } catch {
    return false;
  }
};

const greeting = (date: Date) => {
  const hour = date.getHours();
  if (hour < 13) return "Buongiorno";
  if (hour < 18) return "Buon pomeriggio";
  return "Buonasera";
};

const average = (values: number[]) => (values.length ? values.reduce((s, v) => s + v, 0) / values.length : 0);

/** Variazione percentuale rispetto alla media, come frase breve. */
function VersusAverage({ current, avg, lowerIsBetter }: { current: number; avg: number; lowerIsBetter: boolean }) {
  const a = useTheme().app;
  if (avg <= 0) return <span>Nessuno storico per il confronto</span>;

  const diff = ((current - avg) / avg) * 100;
  const good = lowerIsBetter ? diff <= 0 : diff >= 0;
  return (
    <span>
      <Box component="span" sx={{ fontWeight: 700, color: good ? a.income : a.expense }}>
        {diff >= 0 ? "+" : "−"}
        {formatPercent(Math.abs(diff), 0)}
      </Box>{" "}
      rispetto alla media degli ultimi 6 mesi ({formatCurrency(avg)})
    </span>
  );
}

export default function DashboardPage() {
  const theme = useTheme();
  const a = theme.app;
  const navigate = useNavigate();
  const { userName } = useAuth();
  const { dataVersion } = useTransactions();
  const { openTransactionModal } = useTransactionModal();

  const [loading, setLoading] = useState(true);
  const [data, setData] = useState<LandingDashboard | null>(null);
  // flusso mensile degli ultimi 12 mesi: serve solo per le medie di confronto dei KPI
  const [cashFlow, setCashFlow] = useState<CashFlowPoint[]>([]);
  const [recent, setRecent] = useState<Transaction[]>([]);
  const [recentLoading, setRecentLoading] = useState(true);
  const [balanceVisible, setBalanceVisible] = useState<boolean>(readBalanceVisible);
  const [breakdownKind, setBreakdownKind] = useState<"spent" | "earned">("spent");
  const [breakdownView, setBreakdownView] = useState<BreakdownView>("bars");

  const now = useMemo(() => new Date(), []);

  useEffect(() => {
    const today = new Date();
    const firstMonth = startOfMonth(today, -11);

    const loadLanding = async () => {
      setLoading(true);
      try {
        setData(await TransactionService.getLanding({ excludeTransfers: true }));
      } catch {
        setData(null);
      } finally {
        setLoading(false);
      }
    };

    const loadCashFlow = async () => {
      try {
        const result = await TransactionService.getTimeSeries({
          startDate: firstMonth.toISOString(),
          endDate: today.toISOString(),
          idAccounts: [],
          idCategories: [],
          granularity: 3,
          excludeTransfers: true,
        });
        const byPeriod = new Map((result.series[0]?.values ?? []).map((p) => [p.period, p]));
        // il BE omette i mesi senza movimenti: li riempiamo a zero per non falsare l'asse
        setCashFlow(
          enumerateMonthKeys(firstMonth, today).map((period) => ({
            period,
            earned: byPeriod.get(period)?.earned ?? 0,
            spent: byPeriod.get(period)?.spent ?? 0,
          })),
        );
      } catch {
        setCashFlow([]);
      }
    };

    const loadRecent = async () => {
      setRecentLoading(true);
      try {
        const from = new Date(today);
        from.setDate(from.getDate() - 90);
        const result = await TransactionService.getAll({
          fromDate: from.toISOString(),
          toDate: today.toISOString(),
          page: 0,
          pageSize: 7,
        });
        setRecent(result.items);
      } catch {
        setRecent([]);
      } finally {
        setRecentLoading(false);
      }
    };

    void Promise.all([loadLanding(), loadCashFlow(), loadRecent()]);
  }, [dataVersion]);

  const toggleBalance = () => {
    setBalanceVisible((prev) => {
      try {
        window.localStorage.setItem(BALANCE_VISIBILITY_STORAGE_KEY, String(!prev));
      } catch {
        // ignora
      }
      return !prev;
    });
  };

  const netWorthPoints = useMemo(
    () => (data?.netWorthSeries?.series?.[0]?.values ?? []).map((v) => ({ period: v.period, amount: v.amount })),
    [data],
  );

  const history = useMemo(() => {
    const currentKey = toMonthKey(now);
    const past = cashFlow.filter((p) => p.period !== currentKey).slice(-6);
    return { avgSpent: average(past.map((p) => p.spent)), avgEarned: average(past.map((p) => p.earned)) };
  }, [cashFlow, now]);

  const breakdownItems = useMemo(
    () =>
      (data?.categories ?? []).map((c) => ({
        id: c.categoryId,
        name: c.name,
        value: breakdownKind === "spent" ? c.spentMonth : c.earnedMonth,
      })),
    [data, breakdownKind],
  );

  const breakdownEmpty = breakdownKind === "spent" ? "Nessuna uscita questo mese" : "Nessuna entrata questo mese";

  const asOfText =data?.asOf
    ? new Date(data.asOf).toLocaleString("it-IT", { dateStyle: "short", timeStyle: "short" })
    : "";

  const daysInMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0).getDate();
  const monthProgress = (now.getDate() / daysInMonth) * 100;
  const firstName = userName.split(/[\s@]/)[0];
  const totals = data?.totals;
  const savingsRate = totals && totals.earnedMonth > 0 ? (totals.netMonth / totals.earnedMonth) * 100 : null;

  const gridSx = {
    display: "grid",
    gap: { xs: 2, md: 2.5 },
    gridTemplateColumns: { xs: "1fr", lg: "repeat(12, minmax(0, 1fr))" },
  };

  return (
    <PageContainer>
      <PageHeader
        eyebrow={now.toLocaleDateString("it-IT", { weekday: "long", day: "numeric", month: "long" })}
        title={`${greeting(now)}${firstName ? `, ${firstName}` : ""}`}
        subtitle={`Ecco come stanno andando le tue finanze a ${formatMonthYear(now)}.`}
        actions={
          <Button variant="outlined" startIcon={<InsightsRounded />} onClick={() => navigate(ROUTES.analytics)}>
            Analisi dettagliata
          </Button>
        }
      />

      {!loading && !data && <Alert severity="warning">Impossibile caricare la dashboard. Riprova più tardi.</Alert>}

      {/* Riga 1: patrimonio + KPI del mese */}
      <Box sx={gridSx}>
        <Box sx={{ gridColumn: { lg: "span 5" } }}>
          {loading || !data ? (
            <Skeleton variant="rounded" height={280} sx={{ borderRadius: `${a.radiusCard}px` }} />
          ) : (
            <HeroBalanceCard
              balance={data.totals.currentBalanceTotal}
              series={netWorthPoints}
              hidden={!balanceVisible}
              onToggleHidden={toggleBalance}
              asOfText={asOfText}
            />
          )}
        </Box>
        <Box
          sx={{
            gridColumn: { lg: "span 7" },
            display: "grid",
            gap: { xs: 2, md: 2.5 },
            gridTemplateColumns: "1fr 1fr",
          }}
        >
          {loading || !totals ? (
            Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} variant="rounded" height={130} sx={{ borderRadius: `${a.radiusCard}px` }} />
            ))
          ) : (
            <>
              <StatTile
                label="Entrate del mese"
                tone="income"
                icon={<SouthWestRounded />}
                value={formatCurrency(totals.earnedMonth)}
                footnote={<VersusAverage current={totals.earnedMonth} avg={history.avgEarned} lowerIsBetter={false} />}
              />
              <StatTile
                label="Uscite del mese"
                tone="expense"
                icon={<NorthEastRounded />}
                value={formatCurrency(totals.spentMonth)}
                footnote={<VersusAverage current={totals.spentMonth} avg={history.avgSpent} lowerIsBetter />}
              />
              <StatTile
                label="Risparmio netto"
                tone="accent"
                icon={<SavingsOutlined />}
                value={formatSignedCurrency(totals.netMonth)}
                progress={monthProgress}
                progressLabel={`Giorno ${now.getDate()} di ${daysInMonth}: il mese è al ${formatPercent(monthProgress, 0)}`}
              />
              <StatTile
                label="Tasso di risparmio"
                tone={savingsRate !== null && savingsRate < 0 ? "expense" : "income"}
                icon={<PercentRounded />}
                value={savingsRate === null ? "—" : formatPercent(savingsRate)}
                progress={savingsRate === null ? 0 : Math.max(savingsRate, 0)}
                progressLabel={
                  savingsRate === null
                    ? "Nessuna entrata registrata questo mese"
                    : "Quota delle entrate che resta dopo le spese"
                }
              />
            </>
          )}
        </Box>
      </Box>

      {/* Riga 2: conti + dove vanno i soldi + ultimi movimenti (il flusso di cassa è solo in Analisi) */}
      <Box sx={gridSx}>
        <SurfaceCard sx={{ gridColumn: { lg: "span 4" } }} title="Conti" subtitle="Saldo attuale e movimento del mese">
          {loading || !data ? (
            <Skeleton variant="rounded" height={240} />
          ) : (
            <AccountsOverview accounts={data.accounts} hidden={!balanceVisible} />
          )}
        </SurfaceCard>

        <SurfaceCard
          sx={{ gridColumn: { lg: "span 4" } }}
          title="Dove vanno i soldi"
          subtitle="Per categoria, mese corrente"
          action={
            <ToggleButtonGroup
              size="small"
              exclusive
              value={breakdownView}
              onChange={(_e, value: BreakdownView | null) => value && setBreakdownView(value)}
              aria-label="Vista"
            >
              <ToggleButton value="bars" aria-label="Barre">
                <Tooltip title="Barre">
                  <BarChartRounded sx={{ fontSize: 18, transform: "rotate(90deg)" }} />
                </Tooltip>
              </ToggleButton>
              <ToggleButton value="pie" aria-label="Torta">
                <Tooltip title="Torta">
                  <DonutLargeRounded sx={{ fontSize: 18 }} />
                </Tooltip>
              </ToggleButton>
            </ToggleButtonGroup>
          }
        >
          <ToggleButtonGroup
            size="small"
            exclusive
            value={breakdownKind}
            onChange={(_e, value: "spent" | "earned" | null) => value && setBreakdownKind(value)}
            sx={{ mb: 2 }}
          >
            <ToggleButton value="spent">Uscite</ToggleButton>
            <ToggleButton value="earned">Entrate</ToggleButton>
          </ToggleButtonGroup>
          {loading ? (
            <Skeleton variant="rounded" height={280} />
          ) : breakdownView === "pie" ? (
            <CategoryDonutChart items={breakdownItems} emptyMessage={breakdownEmpty} />
          ) : (
            <RankedBarList
              items={breakdownItems}
              maxItems={Infinity}
              color={breakdownKind === "spent" ? a.expense : a.income}
              emptyMessage={breakdownEmpty}
            />
          )}
        </SurfaceCard>

        <SurfaceCard
          sx={{ gridColumn: { lg: "span 4" } }}
          title="Ultimi movimenti"
          subtitle="Clicca su un movimento per modificarlo"
          action={
            <Button size="small" endIcon={<ArrowForwardRounded />} onClick={() => navigate(ROUTES.transactions)}>
              Vedi tutti
            </Button>
          }
        >
          <RecentTransactions items={recent} loading={recentLoading} onSelect={(t) => openTransactionModal(t)} />
        </SurfaceCard>
      </Box>
    </PageContainer>
  );
}
