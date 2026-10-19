import { useMemo } from "react";
import { Box, Stack, Tooltip, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { LandingAccountBalance } from "../../models/LandingDashboard";
import { buildEntityColorMap } from "../charts/entityColors";
import { formatCurrency, formatPercent, formatSignedCurrency } from "../../utilities/format.utilities";
import EmptyState from "../ui/EmptyState";

interface AccountsOverviewProps {
  accounts: LandingAccountBalance[];
  hidden?: boolean;
}

/** Ripartizione del patrimonio per conto: barra 100% + elenco con saldo e movimento del mese. */
export default function AccountsOverview({ accounts, hidden = false }: AccountsOverviewProps) {
  const a = useTheme().app;

  const colorMap = useMemo(
    () => buildEntityColorMap(accounts.map((x) => x.accountId), a.chart.series, a.chart.other),
    [accounts, a.chart.series, a.chart.other],
  );

  const sorted = useMemo(
    () => [...accounts].sort((x, y) => y.currentBalance - x.currentBalance),
    [accounts],
  );
  const positiveTotal = sorted.reduce((sum, x) => sum + Math.max(x.currentBalance, 0), 0);

  if (accounts.length === 0) {
    return <EmptyState title="Nessun conto" text="Aggiungi un conto dalle impostazioni per iniziare." />;
  }

  const mask = (value: string) => (hidden ? "••••••" : value);

  return (
    <Stack spacing={2.5}>
      {positiveTotal > 0 && (
        <Box
          role="img"
          aria-label="Ripartizione del saldo positivo per conto"
          sx={{ display: "flex", gap: "2px", height: 12, borderRadius: 6, overflow: "hidden" }}
        >
          {sorted
            .filter((x) => x.currentBalance > 0)
            .map((x) => {
              const share = (x.currentBalance / positiveTotal) * 100;
              return (
                <Tooltip key={x.accountId} title={`${x.name} · ${formatPercent(share)}`}>
                  <Box sx={{ flex: `${share} 1 0`, minWidth: 3, backgroundColor: colorMap.get(x.accountId) }} />
                </Tooltip>
              );
            })}
        </Box>
      )}

      <Stack spacing={0.25}>
        {sorted.map((x) => {
          const share = positiveTotal > 0 && x.currentBalance > 0 ? (x.currentBalance / positiveTotal) * 100 : null;
          return (
            <Stack
              key={x.accountId}
              direction="row"
              alignItems="center"
              spacing={1.5}
              sx={{
                py: 1,
                px: 1,
                mx: -1,
                borderRadius: `${a.radiusControl}px`,
                "&:hover": { backgroundColor: a.surfaceAlt },
              }}
            >
              <Box sx={{ width: 10, height: 10, borderRadius: "3px", flexShrink: 0, backgroundColor: colorMap.get(x.accountId) }} />
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Typography sx={{ fontSize: "0.88rem", fontWeight: 600, color: a.text }} noWrap>
                  {x.name}
                </Typography>
                <Typography sx={{ fontSize: "0.74rem", color: a.textMuted }}>
                  {share !== null ? `${formatPercent(share)} del totale` : "—"}
                </Typography>
              </Box>
              <Box sx={{ textAlign: "right" }}>
                <Typography sx={{ fontSize: "0.88rem", fontWeight: 650, color: a.text }}>
                  {mask(formatCurrency(x.currentBalance))}
                </Typography>
                <Typography
                  sx={{ fontSize: "0.74rem", fontWeight: 600, color: x.netMonth >= 0 ? a.income : a.expense }}
                >
                  {formatSignedCurrency(x.netMonth)} questo mese
                </Typography>
              </Box>
            </Stack>
          );
        })}
      </Stack>
    </Stack>
  );
}
