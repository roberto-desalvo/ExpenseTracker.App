import { useEffect, useMemo, useState } from "react";
import { Alert, Box, CircularProgress, Grid, Stack, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import TimeSeriesLineChart, { TimeSeriesLineChartSeries } from "../components/TimeSeriesLineChart";
import CategoriesPieChart from "../components/CategoriesPieChart";
import AccountsPieChart from "../components/AccountsPieChart";
import { LandingDashboard } from "../models/LandingDashboard";
import TransactionService from "../services/TransactionService";

const formatAmount = (value: number) =>
  value.toLocaleString("it-IT", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

function SummaryCard({
  title,
  value,
  color,
}: {
  title: string;
  value: number;
  color?: string;
}) {
  return (
    <Stack
      spacing={0.5}
      sx={{
        borderRadius: 2,
        border: "1px solid",
        borderColor: "divider",
        backgroundColor: "background.paper",
        px: 2,
        py: 1.5,
      }}
    >
      <Typography variant="caption" sx={{ color: "text.secondary", textTransform: "uppercase", letterSpacing: "0.05em" }}>
        {title}
      </Typography>
      <Typography variant="h6" sx={{ fontWeight: 700, color: color ?? "text.primary" }}>
        {value >= 0 ? "+" : "-"} {formatAmount(Math.abs(value))} EUR
      </Typography>
    </Stack>
  );
}

export default function LandingPage() {
  const theme = useTheme();
  const c = theme.palette.custom;
  const [loading, setLoading] = useState<boolean>(true);
  const [data, setData] = useState<LandingDashboard | null>(null);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const result = await TransactionService.getLanding();
        setData(result);
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, []);

  const chartSeries = useMemo<TimeSeriesLineChartSeries[]>(() => {
    if (!data?.netWorthSeries?.series || data.netWorthSeries.series.length === 0) {
      return [];
    }

    const firstSeries = data.netWorthSeries.series[0];
    return [
      {
        name: "Patrimonio totale",
        values: firstSeries.values,
      },
    ];
  }, [data]);

  return (
    <Stack spacing={2.5} sx={{ flex: 1, px: { xs: 2.5, md: 4 }, py: { xs: 2.5, md: 3 } }}>
      <Typography
        variant="h5"
        sx={{
          color: "text.primary",
          fontWeight: 700,
          letterSpacing: "-0.02em",
          px: 1,
        }}
      >
        Home
      </Typography>

      {loading ? (
        <Box sx={{ display: "flex", justifyContent: "center", py: 8 }}>
          <CircularProgress />
        </Box>
      ) : !data ? (
        <Alert severity="warning">Nessun dato disponibile.</Alert>
      ) : (
        <Stack spacing={2.5} sx={{ px: 1 }}>
          <Grid container spacing={1.5}>
            <Grid item xs={12} sm={6} md={3}>
              <SummaryCard title="Saldo totale" value={data.totals.currentBalanceTotal} color={data.totals.currentBalanceTotal >= 0 ? c.amountPositive : c.amountNegative} />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <SummaryCard title="Entrate mese" value={data.totals.earnedMonth} color={c.amountPositive} />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <SummaryCard title="Uscite mese" value={-data.totals.spentMonth} color={c.amountNegative} />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <SummaryCard title="Net mese" value={data.totals.netMonth} color={data.totals.netMonth >= 0 ? c.amountPositive : c.amountNegative} />
            </Grid>
          </Grid>

          <Box
            sx={{
              border: `1px solid ${c.filterBorder}`,
              borderRadius: 2,
              backgroundColor: "background.paper",
              p: 2,
            }}
          >
            <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
              Andamento patrimonio ultimi 12 mesi
            </Typography>
            <TimeSeriesLineChart series={chartSeries} tightYAxis />
          </Box>

          <Grid container spacing={1.5}>
            <Grid item xs={12} md={6}>
              <Box
                sx={{
                  border: `1px solid ${c.filterBorder}`,
                  borderRadius: 2,
                  backgroundColor: "background.paper",
                  p: 2,
                }}
              >
                <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
                  Giacenze account
                </Typography>
                <AccountsPieChart accounts={data.accounts} />
              </Box>
            </Grid>
            <Grid item xs={12} md={6}>
              <Box
                sx={{
                  border: `1px solid ${c.filterBorder}`,
                  borderRadius: 2,
                  backgroundColor: "background.paper",
                  p: 2,
                }}
              >
                <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
                  Categorie (mese corrente)
                </Typography>
                <CategoriesPieChart categories={data.categories} />
              </Box>
            </Grid>
          </Grid>
        </Stack>
      )}
    </Stack>
  );
}
