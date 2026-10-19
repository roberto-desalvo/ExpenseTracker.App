import { useEffect, useMemo, useState } from "react";
import {
  Box,
  Button,
  ButtonBase,
  IconButton,
  Pagination,
  Skeleton,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { AddRounded, EditOutlined } from "@mui/icons-material";
import Account from "../models/Account";
import { LandingAccountBalance } from "../models/LandingDashboard";
import TransactionService from "../services/TransactionService";
import { useAccounts } from "../stores/AccountContext";
import { useSuccessMessage } from "../stores/SuccessMessageContext";
import { useTransactions } from "../stores/TransactionContext";
import EditorDrawer from "../components/ui/EditorDrawer";
import EmptyState from "../components/ui/EmptyState";
import SearchField from "../components/ui/SearchField";
import { buildEntityColorMap } from "../components/charts/entityColors";
import { formatCurrency, formatSignedCurrency, initialsOf } from "../utilities/format.utilities";

export default function AccountsPage() {
  const a = useTheme().app;
  const { showSuccess } = useSuccessMessage();
  const { dataVersion } = useTransactions();
  const {
    accounts,
    pagedAccounts,
    isLoading,
    page,
    pageSize,
    totalCount,
    modifyPage,
    addAccount,
    updateAccount,
    refreshAccounts,
  } = useAccounts();

  const [balances, setBalances] = useState<Map<number, LandingAccountBalance>>(new Map());
  const [balancesLoading, setBalancesLoading] = useState(true);
  const [editorOpen, setEditorOpen] = useState(false);
  const [editing, setEditing] = useState<Account | null>(null);
  const [name, setName] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const load = async () => {
      setBalancesLoading(true);
      try {
        const landing = await TransactionService.getLanding({ excludeTransfers: true });
        setBalances(new Map(landing.accounts.map((x) => [x.accountId, x])));
      } catch {
        setBalances(new Map());
      } finally {
        setBalancesLoading(false);
      }
    };
    void load();
  }, [dataVersion, accounts.length]);

  // stessi colori dei grafici: l'identità del conto resta coerente in tutta l'app
  const colorMap = useMemo(
    () => buildEntityColorMap(accounts.map((x) => x.id), a.chart.series, a.chart.other),
    [accounts, a.chart.series, a.chart.other],
  );

  const openCreate = () => {
    setEditing(null);
    setName("");
    setEditorOpen(true);
  };

  const openEdit = (account: Account) => {
    setEditing(account);
    setName(account.name);
    setEditorOpen(true);
  };

  const handleSubmit = async () => {
    const trimmed = name.trim();
    if (!trimmed) return;
    setBusy(true);
    try {
      if (editing) {
        await updateAccount({ id: editing.id, name: trimmed });
        showSuccess(`Conto "${trimmed}" rinominato`);
      } else {
        await addAccount({ id: 0, name: trimmed });
        showSuccess(`Conto "${trimmed}" creato`);
      }
      setEditorOpen(false);
    } catch {
      // errore mostrato dal layer API
    } finally {
      setBusy(false);
    }
  };

  const pageCount = Math.max(Math.ceil(totalCount / pageSize), 1);

  return (
    <Stack spacing={2}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} alignItems={{ xs: "stretch", sm: "flex-start" }}>
        <SearchField
          placeholder="Cerca conto per nome"
          onSearch={(term) => {
            modifyPage(0);
            void refreshAccounts(term);
          }}
        />
        <Box sx={{ flex: 1 }} />
        <Button variant="contained" startIcon={<AddRounded />} onClick={openCreate}>
          Nuovo conto
        </Button>
      </Stack>

      {isLoading ? (
        <Box sx={{ display: "grid", gap: 2, gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr", lg: "repeat(3, 1fr)" } }}>
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} variant="rounded" height={150} sx={{ borderRadius: `${a.radiusCard}px` }} />
          ))}
        </Box>
      ) : pagedAccounts.length === 0 ? (
        <Box sx={{ border: `1px solid ${a.border}`, borderRadius: `${a.radiusCard}px`, backgroundColor: a.surface }}>
          <EmptyState
            title="Nessun conto trovato"
            text="Aggiungi i tuoi conti correnti, carte e wallet per tenere traccia di dove si trovano i tuoi soldi."
            action={
              <Button variant="contained" startIcon={<AddRounded />} onClick={openCreate}>
                Nuovo conto
              </Button>
            }
            minHeight={300}
          />
        </Box>
      ) : (
        <Box sx={{ display: "grid", gap: 2, gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr", lg: "repeat(3, 1fr)" } }}>
          {pagedAccounts.map((account) => {
            const balance = balances.get(account.id);
            const color = colorMap.get(account.id) ?? a.chart.other;
            return (
              <Box
                key={account.id}
                sx={{
                  position: "relative",
                  backgroundColor: a.surface,
                  border: `1px solid ${a.border}`,
                  borderRadius: `${a.radiusCard}px`,
                  boxShadow: a.cardShadow,
                  p: 2.25,
                  overflow: "hidden",
                  transition: "border-color .15s ease, transform .15s ease",
                  "&:hover": { borderColor: a.borderStrong },
                  "&::before": {
                    content: '""',
                    position: "absolute",
                    left: 0,
                    top: 0,
                    right: 0,
                    height: 3,
                    backgroundColor: color,
                  },
                }}
              >
                <Stack direction="row" spacing={1.5} alignItems="center">
                  <Box
                    sx={{
                      width: 38,
                      height: 38,
                      borderRadius: `${Math.max(a.radiusControl, 6)}px`,
                      display: "grid",
                      placeItems: "center",
                      backgroundColor: a.surfaceAlt,
                      color: a.text,
                      fontWeight: 700,
                      fontSize: "0.8rem",
                      flexShrink: 0,
                    }}
                  >
                    {initialsOf(account.name)}
                  </Box>
                  <Typography sx={{ flex: 1, minWidth: 0, fontWeight: 650, fontSize: "0.95rem" }} noWrap>
                    {account.name}
                  </Typography>
                  <Tooltip title="Rinomina">
                    <IconButton size="small" onClick={() => openEdit(account)} aria-label={`Rinomina ${account.name}`} sx={{ color: a.textSecondary }}>
                      <EditOutlined fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </Stack>

                <Typography sx={{ fontSize: "0.74rem", color: a.textMuted, mt: 2.25 }}>Saldo attuale</Typography>
                {balancesLoading ? (
                  <Skeleton width="55%" height={34} />
                ) : (
                  <Typography
                    sx={{ fontFamily: a.headingFontFamily, fontSize: "1.45rem", fontWeight: 750, letterSpacing: "-0.02em", lineHeight: 1.3 }}
                  >
                    {balance ? formatCurrency(balance.currentBalance) : "—"}
                  </Typography>
                )}
                {balance && !balancesLoading && (
                  <Stack direction="row" spacing={1.5} sx={{ mt: 0.75 }}>
                    <Typography sx={{ fontSize: "0.76rem", color: a.textSecondary }}>
                      Questo mese{" "}
                      <Box component="span" sx={{ fontWeight: 700, color: balance.netMonth >= 0 ? a.income : a.expense }}>
                        {formatSignedCurrency(balance.netMonth)}
                      </Box>
                    </Typography>
                  </Stack>
                )}
              </Box>
            );
          })}

          <ButtonBase
            onClick={openCreate}
            sx={{
              minHeight: 150,
              borderRadius: `${a.radiusCard}px`,
              border: `1.5px dashed ${a.borderStrong}`,
              color: a.textSecondary,
              flexDirection: "column",
              gap: 1,
              "&:hover": { borderColor: a.primary, color: a.primary, backgroundColor: a.primarySoft },
            }}
          >
            <AddRounded />
            <Typography sx={{ fontWeight: 600, fontSize: "0.88rem" }}>Aggiungi conto</Typography>
          </ButtonBase>
        </Box>
      )}

      {pageCount > 1 && (
        <Stack alignItems="center">
          <Pagination count={pageCount} page={page + 1} onChange={(_e, value) => modifyPage(value - 1)} shape="rounded" />
        </Stack>
      )}

      <EditorDrawer
        open={editorOpen}
        onClose={() => setEditorOpen(false)}
        title={editing ? "Rinomina conto" : "Nuovo conto"}
        subtitle={editing ? undefined : "Conto corrente, carta, wallet o investimenti"}
        onSubmit={() => void handleSubmit()}
        isBusy={busy}
        submitDisabled={name.trim().length === 0}
        submitLabel={editing ? "Salva" : "Crea conto"}
      >
        <TextField
          label="Nome del conto"
          required
          autoFocus
          value={name}
          onChange={(e) => setName(e.target.value)}
          disabled={busy}
          placeholder="Es. Conto corrente BBVA"
          helperText="Usa lo stesso nome configurato per l'import, se il conto riceve movimenti da file."
        />
      </EditorDrawer>
    </Stack>
  );
}
