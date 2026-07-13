import { useEffect, useMemo, useState } from "react";
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
import { CHART_SERIES_COLORS } from "../theme/chartColors";

export interface MonthlyDistributionItem {
  id: number;
  name: string;
  amount: number;
}

interface MonthlyDistributionPieChartProps {
  items: MonthlyDistributionItem[];
  amountLabel: string;
  height?: number;
  emptyMessage?: string;
  amountColor?: string;
}

interface PieDataItem extends MonthlyDistributionItem {
  value: number;
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

export default function MonthlyDistributionPieChart({
  items,
  amountLabel,
  height = 280,
  emptyMessage = "Nessun dato disponibile",
  amountColor,
}: MonthlyDistributionPieChartProps) {
  const theme = useTheme();
  const c = theme.palette.custom;
  const [hiddenIds, setHiddenIds] = useState<Set<number>>(new Set());
  const [selectedItemId, setSelectedItemId] = useState<number | null>(null);

  const baseChartData: PieDataItem[] = useMemo(
    () =>
      items
        .map((item) => ({
          ...item,
          value: Math.abs(item.amount),
        }))
        .filter((item) => item.value > 0),
    [items],
  );

  const chartData = useMemo(
    () => baseChartData.filter((item) => !hiddenIds.has(item.id)),
    [baseChartData, hiddenIds],
  );

  const totalValue = chartData.reduce((sum, item) => sum + item.value, 0);

  useEffect(() => {
    if (selectedItemId === null) {
      return;
    }

    const selectedStillVisible = chartData.some((item) => item.id === selectedItemId);
    if (!selectedStillVisible) {
      setSelectedItemId(null);
    }
  }, [chartData, selectedItemId]);

  const selectedItem = selectedItemId === null
    ? null
    : chartData.find((item) => item.id === selectedItemId) ?? null;

  const selectedIndex = selectedItemId === null
    ? -1
    : chartData.findIndex((item) => item.id === selectedItemId);

  const centerValue = selectedItem?.value ?? totalValue;
  const centerLabel = selectedItem?.name ?? "Totale";

  const toggleItem = (id: number) => {
    setHiddenIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const renderTooltip = ({ active, payload }: TooltipProps<number, string>) => {
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
        <Typography variant="body2" sx={{ color: amountColor ?? "text.primary" }}>
          {amountLabel}: {formatAmount(item.value)} EUR
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
        <Typography variant="body2">{emptyMessage}</Typography>
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
            Tutti gli elementi sono nascosti. Clicca sulla legenda per mostrarli.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
          {baseChartData.map((item, index) => {
            const color = CHART_SERIES_COLORS[index % CHART_SERIES_COLORS.length];
            const isHidden = hiddenIds.has(item.id);

            return (
              <ButtonBase
                key={item.id}
                onClick={() => toggleItem(item.id)}
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

  const handleChartAreaClick = () => {
    setSelectedItemId(null);
  };

  const handleSliceClick = (
    item: PieDataItem,
    _index: number,
    event: React.MouseEvent<Element, MouseEvent>,
  ) => {
    event.preventDefault();
    event.stopPropagation();
    setSelectedItemId(item.id);
  };

  return (
    <Stack spacing={1.5}>
      <Box
        sx={{
          width: "100%",
          height,
          position: "relative",
          cursor: "pointer",
          "& .recharts-tooltip-wrapper": {
            zIndex: 20,
          },
          "& .recharts-wrapper:focus, & .recharts-surface:focus, & .recharts-sector:focus": {
            outline: "none",
          },
        }}
        onClick={handleChartAreaClick}
      >
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={chartData}
              onClick={handleSliceClick}
              activeIndex={selectedIndex}
              isAnimationActive={false}
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
                <Sector {...props} outerRadius={Number(props.outerRadius) + 6} />
              )}
            >
              {chartData.map((entry) => {
                const baseIndex = baseChartData.findIndex((item) => item.id === entry.id);
                return (
                  <Cell
                    key={entry.id}
                    fill={CHART_SERIES_COLORS[baseIndex % CHART_SERIES_COLORS.length]}
                  />
                );
              })}
            </Pie>
            <Tooltip content={renderTooltip} />
          </PieChart>
        </ResponsiveContainer>

        <Stack
          spacing={0.2}
          sx={{
            position: "absolute",
            top: "50%",
            left: "50%",
            transform: "translate(-50%, -50%)",
            zIndex: 1,
            alignItems: "center",
            textAlign: "center",
            pointerEvents: "none",
            px: 0.5,
            maxWidth: "62%",
          }}
        >
          <Typography
            variant="caption"
            sx={{
              color: "text.secondary",
              textTransform: "uppercase",
              letterSpacing: "0.06em",
              fontSize: "0.64rem",
            }}
          >
            {centerLabel}
          </Typography>
          <Typography
            sx={{
              color: amountColor ?? "text.primary",
              fontWeight: 700,
              fontSize: "0.94rem",
              lineHeight: 1.2,
            }}
          >
            {formatAmount(centerValue)} EUR
          </Typography>
        </Stack>
      </Box>

      <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
        {baseChartData.map((item, index) => {
          const color = CHART_SERIES_COLORS[index % CHART_SERIES_COLORS.length];
          const isHidden = hiddenIds.has(item.id);

          return (
            <ButtonBase
              key={item.id}
              onClick={() => toggleItem(item.id)}
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
