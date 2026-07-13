import { useMemo, useState } from "react";
import { Box, ButtonBase, Stack, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import {
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Sector,
  Tooltip,
  TooltipProps,
} from "recharts";
import { LandingAccountBalance } from "../models/LandingDashboard";

interface AccountsPieChartProps {
  accounts: LandingAccountBalance[];
  height?: number;
}

interface PieDataItem {
  accountId: number;
  name: string;
  value: number;
  currentBalance: number;
}

const PIE_COLORS = [
  "#4f46e5",
  "#0891b2",
  "#16a34a",
  "#d97706",
  "#db2777",
  "#dc2626",
  "#7c3aed",
  "#0f766e",
  "#334155",
  "#1d4ed8",
  "#059669",
  "#ea580c",
];

const formatAmount = (value: number) =>
  value.toLocaleString("it-IT", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

const formatPercent = (value: number) =>
  value.toLocaleString("it-IT", {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  });

export default function AccountsPieChart({
  accounts,
  height = 320,
}: AccountsPieChartProps) {
  const theme = useTheme();
  const c = theme.palette.custom;
  const [hiddenAccountIds, setHiddenAccountIds] = useState<Set<number>>(new Set());

  const baseChartData: PieDataItem[] = useMemo(
    () =>
      accounts
        .map((account) => ({
          accountId: account.accountId,
          name: account.name,
          value: Math.abs(account.currentBalance),
          currentBalance: account.currentBalance,
        }))
        .filter((account) => account.value > 0),
    [accounts],
  );

  const chartData = useMemo(
    () =>
      baseChartData.filter(
        (account) => !hiddenAccountIds.has(account.accountId),
      ),
    [baseChartData, hiddenAccountIds],
  );

  const totalValue = chartData.reduce((sum, item) => sum + item.value, 0);

  const toggleAccount = (accountId: number) => {
    setHiddenAccountIds((prev) => {
      const next = new Set(prev);
      if (next.has(accountId)) {
        next.delete(accountId);
      } else {
        next.add(accountId);
      }
      return next;
    });
  };

  const renderTooltip = ({
    active,
    payload,
  }: TooltipProps<number, string>) => {
    if (!active || !payload || payload.length === 0) {
      return null;
    }

    const item = payload[0]?.payload as PieDataItem | undefined;
    if (!item) {
      return null;
    }

    const percentage = totalValue > 0 ? (item.value / totalValue) * 100 : 0;

    return (
      <Box
        sx={{
          borderRadius: 1.5,
          border: `1px solid ${c.filterBorder}`,
          backgroundColor: theme.palette.background.paper,
          p: 1.2,
          boxShadow:
            theme.palette.mode === "dark"
              ? "0 8px 24px rgba(0,0,0,0.35)"
              : "0 8px 24px rgba(15,23,42,0.12)",
        }}
      >
        <Typography sx={{ fontWeight: 700, mb: 0.4 }}>{item.name}</Typography>
        <Typography variant="body2" sx={{ color: "text.secondary", mb: 0.4 }}>
          Quota: {formatPercent(percentage)}%
        </Typography>
        <Typography
          variant="body2"
          sx={{ color: item.currentBalance >= 0 ? c.amountPositive : c.amountNegative }}
        >
          Saldo: {item.currentBalance >= 0 ? "+" : "-"} {formatAmount(Math.abs(item.currentBalance))} EUR
        </Typography>
      </Box>
    );
  };

  if (baseChartData.length === 0) {
    return (
      <Box
        sx={{
          minHeight: height,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          color: "text.secondary",
        }}
      >
        <Typography variant="body2">Nessun dato account disponibile</Typography>
      </Box>
    );
  }

  if (chartData.length === 0) {
    return (
      <Stack spacing={1.5}>
        <Box
          sx={{
            minHeight: height,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            color: "text.secondary",
          }}
        >
          <Typography variant="body2">
            Tutti gli account sono nascosti. Clicca sulla legenda per mostrarli.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
          {baseChartData.map((item, index) => {
            const color = PIE_COLORS[index % PIE_COLORS.length];
            const isHidden = hiddenAccountIds.has(item.accountId);

            return (
              <ButtonBase
                key={item.accountId}
                onClick={() => toggleAccount(item.accountId)}
                sx={{
                  borderRadius: 999,
                  border: `1px solid ${c.badgeBorder}`,
                  backgroundColor: c.badgeBackground,
                  px: 1.1,
                  py: 0.45,
                  opacity: isHidden ? 0.55 : 1,
                }}
              >
                <Stack direction="row" spacing={0.8} alignItems="center">
                  <Box
                    sx={{
                      width: 10,
                      height: 10,
                      borderRadius: "50%",
                      backgroundColor: color,
                    }}
                  />
                  <Typography
                    variant="caption"
                    sx={{
                      color: c.badgeText,
                      textDecoration: isHidden ? "line-through" : "none",
                    }}
                  >
                    {item.name}
                  </Typography>
                </Stack>
              </ButtonBase>
            );
          })}
        </Stack>
      </Stack>
    );
  }

  return (
    <Stack spacing={1.5}>
      <Box sx={{ width: "100%", height }}>
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={chartData}
              dataKey="value"
              nameKey="name"
              cx="50%"
              cy="50%"
              outerRadius="78%"
              innerRadius="45%"
              paddingAngle={2}
              stroke={theme.palette.background.paper}
              strokeWidth={2}
              activeShape={(props) => (
                <Sector
                  {...props}
                  outerRadius={Number(props.outerRadius) + 6}
                />
              )}
            >
              {chartData.map((entry) => {
                const baseIndex = baseChartData.findIndex(
                  (item) => item.accountId === entry.accountId,
                );
                return (
                  <Cell
                    key={entry.accountId}
                    fill={PIE_COLORS[baseIndex % PIE_COLORS.length]}
                  />
                );
              })}
            </Pie>
            <Tooltip content={renderTooltip} />
          </PieChart>
        </ResponsiveContainer>
      </Box>

      <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
        {baseChartData.map((item, index) => {
          const color = PIE_COLORS[index % PIE_COLORS.length];
          const isHidden = hiddenAccountIds.has(item.accountId);

          return (
            <ButtonBase
              key={item.accountId}
              onClick={() => toggleAccount(item.accountId)}
              sx={{
                borderRadius: 999,
                border: `1px solid ${c.badgeBorder}`,
                backgroundColor: c.badgeBackground,
                px: 1.1,
                py: 0.45,
                opacity: isHidden ? 0.55 : 1,
              }}
            >
              <Stack direction="row" spacing={0.8} alignItems="center">
                <Box
                  sx={{
                    width: 10,
                    height: 10,
                    borderRadius: "50%",
                    backgroundColor: color,
                  }}
                />
                <Typography
                  variant="caption"
                  sx={{
                    color: c.badgeText,
                    textDecoration: isHidden ? "line-through" : "none",
                  }}
                >
                  {item.name}
                </Typography>
              </Stack>
            </ButtonBase>
          );
        })}
      </Stack>
    </Stack>
  );
}