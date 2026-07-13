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
import { LandingCategorySummary } from "../models/LandingDashboard";
import { CHART_SERIES_COLORS } from "../theme/chartColors";

interface CategoriesPieChartProps {
  categories: LandingCategorySummary[];
  height?: number;
}

interface PieDataItem {
  categoryId: number;
  name: string;
  value: number;
  spentMonth: number;
  earnedMonth: number;
  netMonth: number;
}

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

export default function CategoriesPieChart({
  categories,
  height = 320,
}: CategoriesPieChartProps) {
  const theme = useTheme();
  const c = theme.palette.custom;
  const [hiddenCategoryIds, setHiddenCategoryIds] = useState<Set<number>>(new Set());

  const baseChartData: PieDataItem[] = useMemo(
    () =>
      categories
        .map((category) => ({
          categoryId: category.categoryId,
          name: category.name,
          value: category.spentMonth + category.earnedMonth,
          spentMonth: category.spentMonth,
          earnedMonth: category.earnedMonth,
          netMonth: category.netMonth,
        }))
        .filter((category) => category.value > 0),
    [categories],
  );

  const chartData = useMemo(
    () =>
      baseChartData.filter(
        (category) => !hiddenCategoryIds.has(category.categoryId),
      ),
    [baseChartData, hiddenCategoryIds],
  );

  const totalValue = chartData.reduce((sum, item) => sum + item.value, 0);

  const toggleCategory = (categoryId: number) => {
    setHiddenCategoryIds((prev) => {
      const next = new Set(prev);
      if (next.has(categoryId)) {
        next.delete(categoryId);
      } else {
        next.add(categoryId);
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
        <Typography variant="body2" sx={{ color: c.amountNegative }}>
          Uscite: - {formatAmount(item.spentMonth)} EUR
        </Typography>
        <Typography variant="body2" sx={{ color: c.amountPositive }}>
          Entrate: + {formatAmount(item.earnedMonth)} EUR
        </Typography>
        <Typography
          variant="body2"
          sx={{ color: item.netMonth >= 0 ? c.amountPositive : c.amountNegative }}
        >
          Bilancio: {item.netMonth >= 0 ? "+" : "-"} {formatAmount(Math.abs(item.netMonth))} EUR
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
        <Typography variant="body2">Nessun dato categorie disponibile</Typography>
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
            Tutte le categorie sono nascoste. Clicca sulla legenda per mostrarle.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
          {baseChartData.map((item, index) => {
            const color = CHART_SERIES_COLORS[index % CHART_SERIES_COLORS.length];
            const isHidden = hiddenCategoryIds.has(item.categoryId);

            return (
              <ButtonBase
                key={item.categoryId}
                onClick={() => toggleCategory(item.categoryId)}
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
              activeShape={(props: { outerRadius?: number }) => (
                <Sector
                  {...props}
                  outerRadius={Number(props.outerRadius) + 6}
                />
              )}
            >
              {chartData.map((entry) => {
                const baseIndex = baseChartData.findIndex(
                  (item) => item.categoryId === entry.categoryId,
                );
                return (
                  <Cell
                    key={entry.categoryId}
                    fill={CHART_SERIES_COLORS[baseIndex % CHART_SERIES_COLORS.length]}
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
          const color = CHART_SERIES_COLORS[index % CHART_SERIES_COLORS.length];
          const isHidden = hiddenCategoryIds.has(item.categoryId);

          return (
            <ButtonBase
              key={item.categoryId}
              onClick={() => toggleCategory(item.categoryId)}
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