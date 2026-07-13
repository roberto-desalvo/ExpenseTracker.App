import { useEffect, useMemo, useState } from "react";
import {
  Alert,
  Box,
  Chip,
  CircularProgress,
  FormControl,
  Grid,
  IconButton,
  MenuItem,
  Select,
  TextField,
  Tooltip,
  Stack,
  Typography,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import TimeSeriesLineChart, { TimeSeriesLineChartSeries } from "../components/TimeSeriesLineChart";
import { VisibilityOffRounded, VisibilityRounded, ExpandMoreRounded } from "@mui/icons-material";
import { LandingDashboard } from "../models/LandingDashboard";
import TransactionService from "../services/TransactionService";
import TransactionsSummaryBar from "../components/TransactionsSummaryBar";
import AccountsBar from "../components/AccountsBar";
import ExpenseTable from "../components/ExpenseTable";
import TransactionModal from "../components/TransactionModal";
import MonthlyDistributionPieChart, {
  MonthlyDistributionItem,
} from "../components/MonthlyDistributionPieChart";
import { TimeSeriesList } from "../models/TimeSeries";
import { toIsoDateStart, toIsoDateEnd } from "../utilities/date.utilities";

const BALANCE_VISIBILITY_STORAGE_KEY = "expense-tracker:home-balance-visible";
const QUESTO_MESE_EXPANDED_STORAGE_KEY = "expense-tracker:home-questo-mese-expanded";
const PATRIMONIO_EXPANDED_STORAGE_KEY = "expense-tracker:home-patrimonio-expanded";

const formatAmount = (value: number) =>
  value.toLocaleString("it-IT", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

function SummaryCard({
  title,
  value,
  color,
  hidden = false,
  canToggleVisibility = false,
  onToggleVisibility,
}: {
  title: string;
  value: number;
  color?: string;
  hidden?: boolean;
  canToggleVisibility?: boolean;
  onToggleVisibility?: () => void;
}) {
  const formattedValue =
    hidden
      ? "•••••• EUR"
      : `${value >= 0 ? "+" : "-"} ${formatAmount(Math.abs(value))} EUR`;

  return (
    <Stack
      spacing={0.5}
      sx={{
        borderRadius: 2,
        border: "1px solid",
        borderColor: "divider",
        backgroundColor: "background.paper",
        px: 1.5,
        py: 1,
        minHeight: 84,
        transition: "all .2s ease",
        "&:hover": {
          borderColor: "text.disabled",
          boxShadow: "0 0 0 1px",
          boxShadowColor: "divider",
        },
      }}
    >
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={0.75}>
        <Typography variant="caption" sx={{ color: "text.secondary", textTransform: "uppercase", letterSpacing: "0.05em", fontSize: "0.66rem" }}>
          {title}
        </Typography>
        {canToggleVisibility && onToggleVisibility && (
          <Tooltip title={hidden ? "Mostra saldo" : "Nascondi saldo"}>
            <IconButton
              size="small"
              onClick={onToggleVisibility}
              sx={{ color: "text.secondary", p: 0.3 }}
            >
              {hidden ? <VisibilityOffRounded fontSize="small" /> : <VisibilityRounded fontSize="small" />}
            </IconButton>
          </Tooltip>
        )}
      </Stack>
      <Typography sx={{ fontWeight: 700, fontSize: "1.02rem", lineHeight: 1.25, color: hidden ? "text.secondary" : (color ?? "text.primary") }}>
        {formattedValue}
      </Typography>
    </Stack>
  );
}

export default function LandingPage() {
  const theme = useTheme();
  const c = theme.palette.custom;
  const [loading, setLoading] = useState<boolean>(true);
  const [data, setData] = useState<LandingDashboard | null>(null);
  const [isTotalBalanceVisible, setIsTotalBalanceVisible] = useState<boolean>(() => {
    if (typeof window === "undefined") {
      return false;
    }

    return window.localStorage.getItem(BALANCE_VISIBILITY_STORAGE_KEY) === "true";
  });
  const [isQuestoMeseExpanded, setIsQuestoMeseExpanded] = useState<boolean>(() => {
    if (typeof window === "undefined") {
      return true;
    }

    const stored = window.localStorage.getItem(QUESTO_MESE_EXPANDED_STORAGE_KEY);
    return stored === null ? true : stored === "true";
  });
  const [isPatrimonioExpanded, setIsPatrimonioExpanded] = useState<boolean>(() => {
    if (typeof window === "undefined") {
      return true;
    }

    const stored = window.localStorage.getItem(PATRIMONIO_EXPANDED_STORAGE_KEY);
    return stored === null ? true : stored === "true";
  });

  const setBalanceVisibility = (visible: boolean) => {
    setIsTotalBalanceVisible(visible);
    if (typeof window !== "undefined") {
      window.localStorage.setItem(BALANCE_VISIBILITY_STORAGE_KEY, String(visible));
    }
  };

  const setQuestoMeseExpanded = (expanded: boolean) => {
    setIsQuestoMeseExpanded(expanded);
    if (typeof window !== "undefined") {
      window.localStorage.setItem(QUESTO_MESE_EXPANDED_STORAGE_KEY, String(expanded));
    }
  };

  const setPatrimonioExpanded = (expanded: boolean) => {
    setIsPatrimonioExpanded(expanded);
    if (typeof window !== "undefined") {
      window.localStorage.setItem(PATRIMONIO_EXPANDED_STORAGE_KEY, String(expanded));
    }
  };

  // State for Patrimonio section filters and data
  const [patrimonioDashboardLoading, setPatrimonioDashboardLoading] = useState<boolean>(false);
  const [patrimonioStartDate, setPatrimonioStartDate] = useState<string>(() => {
    const now = new Date();
    const start = new Date(now.getFullYear() - 1, now.getMonth(), now.getDate());
    return start.toISOString().slice(0, 10);
  });
  const [patrimonioEndDate, setPatrimonioEndDate] = useState<string>(() => {
    const now = new Date();
    return now.toISOString().slice(0, 10);
  });
  const [patrimonioGranularity, setPatrimonioGranularity] = useState<number>(3);

  // State for patrimonio accounts series data
  const [patrimonioAccountsData, setPatrimonioAccountsData] = useState<TimeSeriesList | null>(null);
  const [patrimonioAccountsLoading, setPatrimonioAccountsLoading] = useState<boolean>(false);

  // State for patrimonio categories series data
  const [patrimonioCategoriesData, setPatrimonioCategoriesData] = useState<TimeSeriesList | null>(null);
  const [patrimonioCategoriesLoading, setPatrimonioCategoriesLoading] = useState<boolean>(false);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const result = await TransactionService.getLanding({ excludeTransfers: true });
        setData(result);
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, []);

  // Load patrimonio dashboard data
  const handlePatrimonioDashboardLoad = async () => {
    if (!patrimonioStartDate || !patrimonioEndDate || !data) {
      return;
    }

    setPatrimonioDashboardLoading(true);
    setPatrimonioAccountsLoading(true);
    setPatrimonioCategoriesLoading(true);
    try {
      const accountIds = data.accounts.map((a) => a.accountId);
      const categoryIds = data.categories.map((c) => c.categoryId);

      const [, accountsResult, categoriesResult] = await Promise.all([
        TransactionService.getStock({
          startDate: toIsoDateStart(patrimonioStartDate),
          endDate: toIsoDateEnd(patrimonioEndDate),
          idAccounts: [],
          idCategories: [],
          granularity: patrimonioGranularity,
          excludeTransfers: true,
        }),
        TransactionService.getStock({
          startDate: toIsoDateStart(patrimonioStartDate),
          endDate: toIsoDateEnd(patrimonioEndDate),
          idAccounts: accountIds,
          idCategories: [],
          granularity: patrimonioGranularity,
          excludeTransfers: true,
        }),
        TransactionService.getTimeSeries({
          startDate: toIsoDateStart(patrimonioStartDate),
          endDate: toIsoDateEnd(patrimonioEndDate),
          idAccounts: [],
          idCategories: categoryIds,
          granularity: patrimonioGranularity,
          excludeTransfers: true,
        }),
      ]);
      setPatrimonioAccountsData(accountsResult);
      setPatrimonioCategoriesData(categoriesResult);
    } finally {
      setPatrimonioDashboardLoading(false);
      setPatrimonioAccountsLoading(false);
      setPatrimonioCategoriesLoading(false);
    }
  };

  useEffect(() => {
    if (!isPatrimonioExpanded) {
      return;
    }

    void handlePatrimonioDashboardLoad();
  }, [patrimonioStartDate, patrimonioEndDate, patrimonioGranularity, isPatrimonioExpanded, data]);

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

  const accountIncomeItems = useMemo<MonthlyDistributionItem[]>(() => {
    if (!data) {
      return [];
    }

    return data.accounts.map((account) => ({
      id: account.accountId,
      name: account.name,
      amount: account.earnedMonth,
    }));
  }, [data]);

  const accountOutcomeItems = useMemo<MonthlyDistributionItem[]>(() => {
    if (!data) {
      return [];
    }

    return data.accounts.map((account) => ({
      id: account.accountId,
      name: account.name,
      amount: account.spentMonth,
    }));
  }, [data]);

  const categoryIncomeItems = useMemo<MonthlyDistributionItem[]>(() => {
    if (!data) {
      return [];
    }

    return data.categories.map((category) => ({
      id: category.categoryId,
      name: category.name,
      amount: category.earnedMonth,
    }));
  }, [data]);

  const categoryOutcomeItems = useMemo<MonthlyDistributionItem[]>(() => {
    if (!data) {
      return [];
    }

    return data.categories.map((category) => ({
      id: category.categoryId,
      name: category.name,
      amount: category.spentMonth,
    }));
  }, [data]);

  const lastUpdateText = useMemo(() => {
    if (!data?.asOf) {
      return "";
    }

    return new Date(data.asOf).toLocaleString("it-IT", {
      dateStyle: "short",
      timeStyle: "short",
    });
  }, [data]);

  // Computed data for patrimonio accounts series
  const patrimonioAccountsChartSeries = useMemo<TimeSeriesLineChartSeries[]>(() => {
    if (!patrimonioAccountsData) {
      return [];
    }

    const accountSeries = patrimonioAccountsData.series.map((serie, index) => {
      const accountDimension = serie.dimensions.find(
        (dimension) => dimension.key === "AccountId",
      );

      const accountId = accountDimension ? Number(accountDimension.value) : NaN;
      const accountName = Number.isFinite(accountId)
        ? (data?.accounts.find((a) => a.accountId === accountId)?.name ?? `Account ${accountId}`)
        : `Serie ${index + 1}`;

      return {
        name: accountName,
        values: serie.values,
      };
    });

    const totalByPeriod = new Map<string, number>();
    patrimonioAccountsData.series.forEach((serie) => {
      serie.values.forEach((point) => {
        totalByPeriod.set(point.period, (totalByPeriod.get(point.period) ?? 0) + point.amount);
      });
    });

    const totalSeries: TimeSeriesLineChartSeries = {
      name: "Totale",
      values: Array.from(totalByPeriod.entries())
        .map(([period, amount]) => ({ period, amount }))
        .sort((a, b) => a.period.localeCompare(b.period)),
    };

    if (accountSeries.length <= 1) {
      return accountSeries;
    }

    return [totalSeries, ...accountSeries];
  }, [patrimonioAccountsData, data?.accounts]);

  // Computed data for patrimonio categories series
  const patrimonioCategoriesChartSeries = useMemo<TimeSeriesLineChartSeries[]>(() => {
    if (!patrimonioCategoriesData) {
      return [];
    }

    return patrimonioCategoriesData.series.map((serie, index) => {
      const categoryDimension = serie.dimensions.find(
        (dimension) => dimension.key === "CategoryId",
      );

      const categoryId = categoryDimension ? Number(categoryDimension.value) : NaN;
      const categoryName = Number.isFinite(categoryId)
        ? (data?.categories.find((c) => c.categoryId === categoryId)?.name ?? `Categoria ${categoryId}`)
        : `Serie ${index + 1}`;

      return {
        name: categoryName,
        values: serie.values,
      };
    });
  }, [patrimonioCategoriesData, data?.categories]);

  const sectionShellSx = {
    border: `1px solid ${c.filterBorder}`,
    borderRadius: 3,
    backgroundColor: "background.paper",
    px: { xs: 1.25, md: 1.5 },
    py: { xs: 1.25, md: 1.5 },
    boxShadow:
      theme.palette.mode === "dark"
        ? "0 8px 24px rgba(0,0,0,0.2)"
        : "0 8px 20px rgba(15,23,42,0.05)",
  };

  const compactFilterBoxSx = {
    borderRadius: "12px",
    border: `1px solid ${c.filterBorder}`,
    backgroundColor: c.filterBackground,
    minWidth: { xs: "100%", sm: 180 },
  };

  const compactSelectSx = {
    ".MuiOutlinedInput-notchedOutline": { border: "none" },
    ".MuiSelect-select": { py: 1, fontSize: "0.88rem" },
    ".MuiSelect-icon": { color: c.filterText },
  };

  const compactMenuProps = {
    PaperProps: {
      sx: {
        mt: 0.75,
        borderRadius: "12px",
        border: `1px solid ${c.drawerBorder}`,
        backgroundColor: c.drawerBackground,
        color: "text.primary",
      },
    },
  };

  const innerPanelSx = {
    border: `1px solid ${c.filterBorder}`,
    borderRadius: 2,
    backgroundColor: "background.paper",
    p: 1.5,
    boxShadow: theme.palette.mode === "dark"
      ? "0 8px 24px rgba(0,0,0,0.2)"
      : "0 8px 20px rgba(15,23,42,0.05)",
  };

  const miniChartCardSx = {
    border: `1px solid ${c.filterBorder}`,
    borderRadius: 2,
    p: 1.25,
    minHeight: 336,
  };

  return (
    <Stack
      spacing={2}
      sx={{
        flex: 1,
        width: "100%",
        maxWidth: 1480,
        mx: "auto",
        px: { xs: 1.5, md: 3 },
        py: { xs: 2, md: 2.5 },
      }}
    >
      <Typography
        variant="h5"
        sx={{
          color: "text.primary",
          fontWeight: 700,
          letterSpacing: "-0.02em",
          px: 1,
        }}
      >
        Dashboard
      </Typography>

      {loading ? (
        <Box sx={{ display: "flex", justifyContent: "center", py: 8 }}>
          <CircularProgress />
        </Box>
      ) : !data ? (
        <Alert severity="warning">Nessun dato disponibile.</Alert>
      ) : (
        <Stack spacing={2} sx={{ px: 1 }}>
          <Stack spacing={2} sx={sectionShellSx}>
            <Stack
              direction={{ xs: "column", sm: "row" }}
              alignItems={{ xs: "flex-start", sm: "flex-start" }}
              justifyContent="space-between"
              spacing={1}
              sx={{ px: 0.5 }}
            >
              <Box>
                <Typography variant="h6" sx={{ fontWeight: 700 }}>
                  Questo mese
                </Typography>
                <Typography variant="body2" sx={{ color: "text.secondary" }}>
                  Sintesi, operazioni e distribuzione per account/categorie
                </Typography>
              </Box>

              <Tooltip title={isQuestoMeseExpanded ? "Comprimi" : "Espandi"}>
                <IconButton
                  onClick={() => setQuestoMeseExpanded(!isQuestoMeseExpanded)}
                  sx={{
                    alignSelf: "flex-start",
                    transform: isQuestoMeseExpanded ? "rotate(0deg)" : "rotate(-90deg)",
                    transition: "transform 0.3s ease",
                    color: "text.secondary",
                  }}
                >
                  <ExpandMoreRounded />
                </IconButton>
              </Tooltip>
            </Stack>

            {isQuestoMeseExpanded && (
              <Box sx={{ overflow: "hidden", animation: "slideDown 0.3s ease-out" }}>
                <Box sx={{ px: 0.5, pb: 1 }}>
                  <Chip
                    label={`Aggiornato: ${lastUpdateText}`}
                    size="small"
                    sx={{
                      borderRadius: 1.5,
                      border: `1px solid ${c.filterBorder}`,
                      backgroundColor: c.filterBackground,
                      color: "text.secondary",
                    }}
                  />
                </Box>
                <Grid container spacing={1.25}>
              <Grid item xs={12} sm={6} md={3}>
                <SummaryCard
                  title="Saldo totale"
                  value={data.totals.currentBalanceTotal}
                  color={data.totals.currentBalanceTotal >= 0 ? c.amountPositive : c.amountNegative}
                  hidden={!isTotalBalanceVisible}
                  canToggleVisibility
                  onToggleVisibility={() => setBalanceVisibility(!isTotalBalanceVisible)}
                />
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <SummaryCard title="Entrate mese" value={data.totals.earnedMonth} color={c.amountPositive} />
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <SummaryCard title="Uscite mese" value={-data.totals.spentMonth} color={c.amountNegative} />
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <SummaryCard title="Bilancio mese" value={data.totals.netMonth} color={data.totals.netMonth >= 0 ? c.amountPositive : c.amountNegative} />
              </Grid>
            </Grid>

            <Box
              sx={{
                mt: 1.25,
                border: `1px solid ${c.filterBorder}`,
                borderRadius: 2,
                backgroundColor: "background.paper",
                p: 1.25,
                boxShadow: "none",
              }}
            >
              <Typography sx={{ px: 2, pt: 1, pb: 0.2, fontWeight: 700, color: "text.secondary", fontSize: "0.88rem" }}>
                Transazioni mese corrente
              </Typography>
              <TransactionsSummaryBar showChips={false} />
              <AccountsBar />
              <Box component="main" sx={{ px: 1, pb: 1 }}>
                <ExpenseTable />
              </Box>
            </Box>

            <Box sx={{ mt: 1.25 }}>
              <Grid container spacing={1.25}>
                <Grid item xs={12} md={6} sx={{ display: "flex" }}>
                  <Box
                    sx={{ ...sectionShellSx, width: "100%" }}
                  >
                    <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1.5 }}>
                      Account
                    </Typography>
                    <Grid container spacing={1.25}>
                      <Grid item xs={12} lg={6}>
                        <Box sx={miniChartCardSx}>
                          <Typography sx={{ fontWeight: 600, mb: 1, color: c.amountPositive }}>
                            Entrate
                          </Typography>
                          <MonthlyDistributionPieChart
                            items={accountIncomeItems}
                            amountLabel="Entrate"
                            amountColor={c.amountPositive}
                            emptyMessage="Nessuna entrata disponibile"
                            height={240}
                          />
                        </Box>
                      </Grid>
                      <Grid item xs={12} lg={6}>
                        <Box sx={miniChartCardSx}>
                          <Typography sx={{ fontWeight: 600, mb: 1, color: c.amountNegative }}>
                            Uscite
                          </Typography>
                          <MonthlyDistributionPieChart
                            items={accountOutcomeItems}
                            amountLabel="Uscite"
                            amountColor={c.amountNegative}
                            emptyMessage="Nessuna uscita disponibile"
                            height={240}
                          />
                        </Box>
                      </Grid>
                    </Grid>
                  </Box>
                </Grid>

                <Grid item xs={12} md={6} sx={{ display: "flex" }}>
                  <Box
                    sx={{ ...sectionShellSx, width: "100%" }}
                  >
                    <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1.5 }}>
                      Categorie
                    </Typography>
                    <Grid container spacing={1.25}>
                      <Grid item xs={12} lg={6}>
                        <Box sx={miniChartCardSx}>
                          <Typography sx={{ fontWeight: 600, mb: 1, color: c.amountPositive }}>
                            Entrate
                          </Typography>
                          <MonthlyDistributionPieChart
                            items={categoryIncomeItems}
                            amountLabel="Entrate"
                            amountColor={c.amountPositive}
                            emptyMessage="Nessuna entrata disponibile"
                            height={240}
                          />
                        </Box>
                      </Grid>
                      <Grid item xs={12} lg={6}>
                        <Box sx={miniChartCardSx}>
                          <Typography sx={{ fontWeight: 600, mb: 1, color: c.amountNegative }}>
                            Uscite
                          </Typography>
                          <MonthlyDistributionPieChart
                            items={categoryOutcomeItems}
                            amountLabel="Uscite"
                            amountColor={c.amountNegative}
                            emptyMessage="Nessuna uscita disponibile"
                            height={240}
                          />
                        </Box>
                      </Grid>
                    </Grid>
                  </Box>
                </Grid>
              </Grid>
            </Box>
              </Box>
            )}
          </Stack>

          <Stack spacing={1.5} sx={sectionShellSx}>
            <Stack
              direction={{ xs: "column", sm: "row" }}
              alignItems={{ xs: "flex-start", sm: "flex-start" }}
              justifyContent="space-between"
              spacing={1}
              sx={{ px: 0.5 }}
            >
              <Box>
                <Typography variant="h6" sx={{ fontWeight: 700 }}>
                  Patrimonio
                </Typography>
                <Typography variant="body2" sx={{ color: "text.secondary" }}>
                  Andamento complessivo e distribuzione per account/categorie
                </Typography>
              </Box>

              <Tooltip title={isPatrimonioExpanded ? "Comprimi" : "Espandi"}>
                <IconButton
                  onClick={() => setPatrimonioExpanded(!isPatrimonioExpanded)}
                  sx={{
                    alignSelf: "flex-start",
                    transform: isPatrimonioExpanded ? "rotate(0deg)" : "rotate(-90deg)",
                    transition: "transform 0.3s ease",
                    color: "text.secondary",
                  }}
                >
                  <ExpandMoreRounded />
                </IconButton>
              </Tooltip>
            </Stack>
            {isPatrimonioExpanded && (
              <Box sx={{ overflow: "hidden", animation: "slideDown 0.3s ease-out" }}>
                <Stack spacing={2}>
                  {/* Filters */}
                  <Stack
                    direction={{ xs: "column", md: "row" }}
                    spacing={1.25}
                    sx={{
                      p: 1,
                      borderRadius: 2,
                      border: `1px solid ${c.filterBorder}`,
                      backgroundColor: c.filterBackground,
                    }}
                  >
                    <TextField
                      type="date"
                      value={patrimonioStartDate}
                      onChange={(event) => setPatrimonioStartDate(event.target.value)}
                      size="small"
                      sx={compactFilterBoxSx}
                      slotProps={{ htmlInput: { "aria-label": "Data inizio" } }}
                    />
                    <TextField
                      type="date"
                      value={patrimonioEndDate}
                      onChange={(event) => setPatrimonioEndDate(event.target.value)}
                      size="small"
                      sx={compactFilterBoxSx}
                      slotProps={{ htmlInput: { "aria-label": "Data fine" } }}
                    />
                    <FormControl size="small" sx={compactFilterBoxSx}>
                      <Select
                        displayEmpty
                        value={String(patrimonioGranularity)}
                        onChange={(event) =>
                          setPatrimonioGranularity(Number(event.target.value))
                        }
                        MenuProps={compactMenuProps}
                        sx={compactSelectSx}
                        renderValue={(selected) => {
                          const selectedGranularity = Number(selected);
                          if (selectedGranularity === 1) {
                            return "Giornaliero";
                          }
                          if (selectedGranularity === 2) {
                            return "Settimanale";
                          }
                          if (selectedGranularity === 3) {
                            return "Mensile";
                          }

                          return "Annuale";
                        }}
                      >
                        <MenuItem value={"1"}>Giornaliero</MenuItem>
                        <MenuItem value={"2"}>Settimanale</MenuItem>
                        <MenuItem value={"3"}>Mensile</MenuItem>
                        <MenuItem value={"4"}>Annuale</MenuItem>
                      </Select>
                    </FormControl>
                  </Stack>

                  {patrimonioStartDate > patrimonioEndDate && (
                    <Alert severity="warning">
                      La data di inizio deve essere precedente o uguale alla data di fine.
                    </Alert>
                  )}

                  {/* Net worth chart */}
                  <Box
                    sx={innerPanelSx}
                  >
                    <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
                      Andamento patrimonio {patrimonioStartDate} - {patrimonioEndDate}
                    </Typography>
                    {patrimonioDashboardLoading ? (
                      <Box
                        sx={{
                          height: 350,
                          display: "flex",
                          justifyContent: "center",
                          alignItems: "center",
                        }}
                      >
                        <CircularProgress size={28} />
                      </Box>
                    ) : (
                      <TimeSeriesLineChart series={chartSeries} tightYAxis />
                    )}
                  </Box>

                  {/* Account charts */}
                  <Box
                    sx={{ ...innerPanelSx, minHeight: 412 }}
                  >
                    <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
                      Account
                    </Typography>
                    {patrimonioAccountsLoading ? (
                      <Box
                        sx={{
                          height: 350,
                          display: "flex",
                          justifyContent: "center",
                          alignItems: "center",
                        }}
                      >
                        <CircularProgress size={28} />
                      </Box>
                    ) : patrimonioAccountsData && patrimonioAccountsChartSeries.length > 0 ? (
                      <TimeSeriesLineChart series={patrimonioAccountsChartSeries} enableLegendToggle />
                    ) : (
                      <Box
                        sx={{
                          height: 350,
                          display: "flex",
                          justifyContent: "center",
                          alignItems: "center",
                          color: "text.secondary",
                        }}
                      >
                        <Typography>Nessun dato disponibile</Typography>
                      </Box>
                    )}
                  </Box>

                  {/* Category charts */}
                  <Box
                    sx={{ ...innerPanelSx, minHeight: 412 }}
                  >
                    <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
                      Categorie
                    </Typography>
                    {patrimonioCategoriesLoading ? (
                      <Box
                        sx={{
                          height: 350,
                          display: "flex",
                          justifyContent: "center",
                          alignItems: "center",
                        }}
                      >
                        <CircularProgress size={28} />
                      </Box>
                    ) : patrimonioCategoriesData && patrimonioCategoriesChartSeries.length > 0 ? (
                      <TimeSeriesLineChart series={patrimonioCategoriesChartSeries} emptyMessage="Nessuna serie disponibile per i filtri selezionati" />
                    ) : (
                      <Box
                        sx={{
                          height: 350,
                          display: "flex",
                          justifyContent: "center",
                          alignItems: "center",
                          color: "text.secondary",
                        }}
                      >
                        <Typography>Nessun dato disponibile</Typography>
                      </Box>
                    )}
                  </Box>
                </Stack>
              </Box>
            )}
          </Stack>

          <TransactionModal />
        </Stack>
      )}
    </Stack>
  );
}
