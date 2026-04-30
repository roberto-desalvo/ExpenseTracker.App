import { useState, useEffect, useMemo } from "react";
import { Box, Stack, Tab, TableCell, TableRow, Tabs, TextField, Typography, FormControl, InputLabel, Select, MenuItem, Checkbox, ListItemText, OutlinedInput, Alert, CircularProgress } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { EditRounded } from "@mui/icons-material";
import Account from "../models/Account";
import DataTableBase, { DataTableColumn } from "../components/DataTableBase";
import RowActionsMenu from "../components/RowActionsMenu";
import AppModal from "../components/AppModal";
import AccountsFilterBar from "../components/AccountsFilterBar";
import TimeSeriesLineChart, { TimeSeriesLineChartSeries } from "../components/TimeSeriesLineChart";
import { useAccounts } from "../stores/AccountContext";
import TransactionService from "../services/TransactionService";
import { TimeSeriesList } from "../models/TimeSeries";
import { toIsoDateStart, toIsoDateEnd } from "../utilities/date.utilities";

type AccountFormState = {
  name: string;
};

const createInitialForm = (): AccountFormState => ({
  name: "",
});

export default function AccountsPage() {
  const theme = useTheme();
  const c = theme.palette.custom;
  const {
    pagedAccounts,
    isLoading,
    page,
    pageSize,
    totalCount,
    modifyPage,
    modifyPageSize,
    addAccount,
    updateAccount,
    refreshAccounts,
  } = useAccounts();

  const [form, setForm] = useState<AccountFormState>(createInitialForm());
  const [modalOpen, setModalOpen] = useState<boolean>(false);
  const [editingAccount, setEditingAccount] = useState<Account | null>(null);
  const [operationInProgress, setOperationInProgress] = useState<boolean>(false);
  const [activeTab, setActiveTab] = useState<number>(0);
  const [dashboardLoading, setDashboardLoading] = useState<boolean>(false);
  const [dashboardData, setDashboardData] = useState<TimeSeriesList | null>(null);
  const [dashboardStartDate, setDashboardStartDate] = useState<string>(() => {
    const now = new Date();
    const start = new Date(now.getFullYear() - 1, now.getMonth(), now.getDate());
    return start.toISOString().slice(0, 10);
  });
  const [dashboardEndDate, setDashboardEndDate] = useState<string>(() => {
    const now = new Date();
    return now.toISOString().slice(0, 10);
  });
  const [dashboardGranularity, setDashboardGranularity] = useState<number>(3);
  const [dashboardAccountIds, setDashboardAccountIds] = useState<number[]>([]);

  const columns: DataTableColumn[] = [
    { id: "name", label: "Nome", minWidth: 200 },
    { id: "actions", label: "Azioni", minWidth: 80, align: "center" },
  ];

  useEffect(() => {
    if (pagedAccounts.length === 0) {
      setDashboardAccountIds([]);
      return;
    }

    setDashboardAccountIds((prev) => {
      if (prev.length === 0) {
        return pagedAccounts.map((account) => account.id);
      }

      return prev.filter((id) => pagedAccounts.some((account) => account.id === id));
    });
  }, [pagedAccounts]);

  const accountNameById = useMemo(
    () => new Map(pagedAccounts.map((account) => [account.id, account.name])),
    [pagedAccounts],
  );

  const chartSeries = useMemo<TimeSeriesLineChartSeries[]>(() => {
    if (!dashboardData) {
      return [];
    }

    const accountSeries = dashboardData.series.map((serie, index) => {
      const accountDimension = serie.dimensions.find(
        (dimension) => dimension.key === "AccountId",
      );

      const accountId = accountDimension ? Number(accountDimension.value) : NaN;
      const accountName = Number.isFinite(accountId)
        ? (accountNameById.get(accountId) ?? `Account ${accountId}`)
        : `Serie ${index + 1}`;

      return {
        name: accountName,
        values: serie.values,
      };
    });

    const totalByPeriod = new Map<string, number>();
    dashboardData.series.forEach((serie) => {
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
  }, [dashboardData, accountNameById]);

  const handleDashboardLoad = async () => {
    if (!dashboardStartDate || !dashboardEndDate) {
      return;
    }

    setDashboardLoading(true);
    try {
      const result = await TransactionService.getStock({
        startDate: toIsoDateStart(dashboardStartDate),
        endDate: toIsoDateEnd(dashboardEndDate),
        idAccounts: dashboardAccountIds.length > 0 ? dashboardAccountIds : [],
        idCategories: [],
        granularity: dashboardGranularity,
      });
      setDashboardData(result);
    } finally {
      setDashboardLoading(false);
    }
  };

  useEffect(() => {
    if (activeTab !== 1) {
      return;
    }

    void handleDashboardLoad();
  }, [activeTab]);

  useEffect(() => {
    if (activeTab !== 1 || !dashboardStartDate || !dashboardEndDate) {
      return;
    }

    void handleDashboardLoad();
  }, [dashboardStartDate, dashboardEndDate, dashboardGranularity, dashboardAccountIds, activeTab]);

  const handleSearch = (name?: string) => {
    modifyPage(0);
    void refreshAccounts(name);
  };

  const openCreateModal = () => {
    setEditingAccount(null);
    setForm(createInitialForm());
    setModalOpen(true);
  };

  const openEditModal = (account: Account) => {
    setEditingAccount(account);
    setForm({
      name: account.name,
    });
    setModalOpen(true);
  };

  const closeModal = () => {
    setModalOpen(false);
  };

  const handleSubmit = async (event: React.FormEvent): Promise<string | void> => {
    event.preventDefault();

    const payload: Account = {
      id: editingAccount?.id ?? 0,
      name: form.name.trim(),
    };

    if (payload.name.length === 0) {
      return;
    }

    setOperationInProgress(true);
    try {
      if (payload.id > 0) {
        await updateAccount(payload);
        return `Account "${payload.name}" modificato`;
      }

      await addAccount(payload);
      return `Account "${payload.name}" creato`;
    } finally {
      setOperationInProgress(false);
    }
  };

  const renderAccountRow = (account: Account) => (
    <TableRow
      hover
      key={account.id}
      sx={{
        "& td:first-of-type": {
          borderTopLeftRadius: 8,
          borderBottomLeftRadius: 8,
        },
        "& td:last-of-type": {
          borderTopRightRadius: 8,
          borderBottomRightRadius: 8,
        },
        "&:hover td": {
          backgroundColor: c.rowHover,
        },
      }}
    >
      <TableCell
        sx={{
          borderBottom: "none",
          backgroundColor: c.rowBackground,
          py: 1.5,
        }}
      >
        <Stack direction="row" spacing={1} alignItems="center">
          <Typography sx={{ color: "text.primary", fontWeight: 500 }}>
            {account.name}
          </Typography>
        </Stack>
      </TableCell>
      <TableCell
        align="center"
        sx={{
          borderBottom: "none",
          backgroundColor: c.rowBackground,
          py: 1.5,
        }}
      >
        <RowActionsMenu
          rowId={account.id}
          ariaLabel="Azioni account"
          actions={[
            {
              label: "Modifica",
              icon: <EditRounded fontSize="small" />,
              onClick: () => openEditModal(account),
            },
          ]}
        />
      </TableCell>
    </TableRow>
  );

  return (
    <>
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
          Account
        </Typography>
        <Box sx={{ px: { xs: 0.5, md: 1 } }}>
          <Tabs
            value={activeTab}
            onChange={(_event, value: number) => setActiveTab(value)}
            textColor="inherit"
            indicatorColor="primary"
          >
            <Tab label="Gestione" />
            <Tab label="Analisi" />
          </Tabs>
        </Box>
        {activeTab === 0 && (
          <AccountsFilterBar
            onSearch={handleSearch}
            onAddClick={openCreateModal}
            onRefresh={() => void refreshAccounts()}
            isLoading={isLoading}
          />
        )}
        <main className="px-2 pb-2">
          {activeTab === 0 ? (
            <DataTableBase
              title="Account"
              columns={columns}
              rows={pagedAccounts}
              isLoading={isLoading}
              isEmpty={!isLoading && pagedAccounts.length === 0}
              emptyMessage="Nessun account trovato"
              emptySubtext="Crea il tuo primo account per iniziare"
              page={page}
              pageSize={pageSize}
              totalCount={totalCount}
              onPageChange={(_event, newPage) => modifyPage(newPage)}
              onPageSizeChange={(event) =>
                modifyPageSize(parseInt(event.target.value, 10))
              }
              renderRow={(account) => renderAccountRow(account)}
            />
          ) : (
            <Box sx={{ p: 2 }}>
            <Stack spacing={2.5}>
              <Stack direction={{ xs: "column", md: "row" }} spacing={1.5}>
                <TextField
                  label="Data inizio"
                  type="date"
                  value={dashboardStartDate}
                  onChange={(event) => setDashboardStartDate(event.target.value)}
                  InputLabelProps={{ shrink: true }}
                  size="small"
                />
                <TextField
                  label="Data fine"
                  type="date"
                  value={dashboardEndDate}
                  onChange={(event) => setDashboardEndDate(event.target.value)}
                  InputLabelProps={{ shrink: true }}
                  size="small"
                />

                <FormControl size="small" sx={{ minWidth: 180 }}>
                  <InputLabel id="granularity-label">Granularità</InputLabel>
                  <Select
                    labelId="granularity-label"
                    value={dashboardGranularity}
                    label="Granularità"
                    onChange={(event) =>
                      setDashboardGranularity(Number(event.target.value))
                    }
                  >
                    <MenuItem value={1}>Giornaliero</MenuItem>
                    <MenuItem value={2}>Settimanale</MenuItem>
                    <MenuItem value={3}>Mensile</MenuItem>
                    <MenuItem value={4}>Annuale</MenuItem>
                  </Select>
                </FormControl>
              </Stack>

              <Stack direction={{ xs: "column", md: "row" }} spacing={1.5}>
                <FormControl size="small" sx={{ minWidth: 280, flex: 1.3 }}>
                  <InputLabel id="dashboard-account-filter-label">Account</InputLabel>
                  <Select
                    labelId="dashboard-account-filter-label"
                    multiple
                    value={dashboardAccountIds.map(String)}
                    onChange={(event) => {
                      const value = event.target.value;
                      const ids =
                        typeof value === "string"
                          ? value.split(",").map((id) => Number(id))
                          : value.map((id) => Number(id));
                      setDashboardAccountIds(ids);
                    }}
                    input={<OutlinedInput label="Account" />}
                    renderValue={(selected) => {
                      if (selected.length === 0) return "Nessun account";
                      return selected
                        .map((id) =>
                          pagedAccounts.find((account) => account.id === Number(id))?.name,
                        )
                        .filter(Boolean)
                        .join(", ");
                    }}
                  >
                    {pagedAccounts.map((account) => (
                      <MenuItem key={account.id} value={String(account.id)}>
                        <Checkbox checked={dashboardAccountIds.includes(account.id)} />
                        <ListItemText primary={account.name} />
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Stack>

              {dashboardStartDate > dashboardEndDate && (
                <Alert severity="warning">
                  La data di inizio deve essere precedente o uguale alla data di fine.
                </Alert>
              )}

              <Box
                sx={{
                  border: `1px solid ${c.filterBorder}`,
                  borderRadius: 2,
                  backgroundColor: theme.palette.background.paper,
                  p: 2,
                  minHeight: 430,
                }}
              >
                {dashboardLoading ? (
                  <Box
                    sx={{
                      display: "flex",
                      justifyContent: "center",
                      alignItems: "center",
                      height: "100%",
                    }}
                  >
                    <CircularProgress />
                  </Box>
                ) : dashboardData && chartSeries.length > 0 ? (
                  <TimeSeriesLineChart series={chartSeries} enableLegendToggle />
                ) : (
                  <Box
                    sx={{
                      display: "flex",
                      justifyContent: "center",
                      alignItems: "center",
                      height: "100%",
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

          <AppModal
            open={modalOpen}
            onClose={closeModal}
            title={editingAccount ? "Modifica account" : "Nuovo account"}
            onSubmit={handleSubmit}
            isBusy={operationInProgress}
            submitLabel={editingAccount ? "Salva modifiche" : "Crea account"}
          >
            <TextField
              label="Nome"
              value={form.name}
              onChange={(event) =>
                setForm((prev) => ({ ...prev, name: event.target.value }))
              }
              required
              fullWidth
              disabled={operationInProgress}
            />
          </AppModal>
        </main>
      </Stack>
    </>
  );
}
