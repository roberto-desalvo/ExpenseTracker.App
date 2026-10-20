import { useMemo } from "react";
import { Box, Stack, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip, TooltipProps } from "recharts";
import { formatCurrency, formatPercent } from "../../utilities/format.utilities";
import { ChartEmpty, ChartTooltipCard, ChartTooltipRow } from "./ChartParts";
import type { RankedItem } from "./RankedBarList";

interface CategoryDonutChartProps {
  items: RankedItem[];
  /** fette mostrate prima di raggruppare il resto in "Altre" (inclusa) */
  maxSlices?: number;
  otherLabel?: string;
  emptyMessage?: string;
  minHeight?: number;
}

interface Slice {
  id: number | string;
  name: string;
  value: number;
  share: number;
  color: string;
}

/**
 * Composizione di un totale (parti di un intero) come ciambella. Le fette sono poche
 * (coda in "Altre") e colorate nell'ordine categoriale fisso, dalla più grande.
 */
export default function CategoryDonutChart({
  items,
  maxSlices = 6,
  otherLabel = "Altre",
  emptyMessage = "Nessun movimento nel periodo",
  minHeight = 260,
}: CategoryDonutChartProps) {
  const a = useTheme().app;

  const { slices, total } = useMemo(() => {
    const positive = items
      .map((item) => ({ ...item, value: Math.abs(item.value) }))
      .filter((item) => item.value > 0)
      .sort((x, y) => y.value - x.value);
    const sum = positive.reduce((acc, item) => acc + item.value, 0);
    const head = positive.length <= maxSlices ? positive : positive.slice(0, maxSlices - 1);
    const tail = positive.slice(head.length);

    const result: Slice[] = head.map((item, index) => ({
      ...item,
      share: sum > 0 ? (item.value / sum) * 100 : 0,
      color: a.chart.series[index] ?? a.chart.other,
    }));
    if (tail.length) {
      const value = tail.reduce((acc, item) => acc + item.value, 0);
      result.push({
        id: "__other__",
        name: `${otherLabel} (${tail.length})`,
        value,
        share: sum > 0 ? (value / sum) * 100 : 0,
        color: a.chart.other,
      });
    }
    return { slices: result, total: sum };
  }, [items, maxSlices, otherLabel, a.chart.series, a.chart.other]);

  if (slices.length === 0) {
    return <ChartEmpty height={minHeight} message={emptyMessage} />;
  }

  const renderTooltip = ({ active, payload }: TooltipProps<number, string>) => {
    if (!active || !payload?.length) return null;
    const slice = payload[0].payload as Slice;
    return (
      <ChartTooltipCard title={slice.name}>
        <ChartTooltipRow color={slice.color} label="Importo" value={formatCurrency(slice.value)} emphasis />
        <ChartTooltipRow label="Quota del totale" value={formatPercent(slice.share)} />
      </ChartTooltipCard>
    );
  };

  return (
    <Stack spacing={2} sx={{ minHeight }}>
      <Box sx={{ position: "relative", width: "100%", height: 190, "& .recharts-surface:focus": { outline: "none" } }}>
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={slices}
              dataKey="value"
              nameKey="name"
              innerRadius="62%"
              outerRadius="96%"
              startAngle={90}
              endAngle={-270}
              stroke={a.surface}
              strokeWidth={2}
              isAnimationActive={false}
            >
              {slices.map((slice) => (
                <Cell key={slice.id} fill={slice.color} />
              ))}
            </Pie>
            <Tooltip content={renderTooltip} />
          </PieChart>
        </ResponsiveContainer>
        {/* totale al centro della ciambella */}
        <Box
          sx={{
            position: "absolute",
            inset: 0,
            display: "grid",
            placeItems: "center",
            pointerEvents: "none",
            textAlign: "center",
          }}
        >
          <Box>
            <Typography sx={{ fontSize: "0.72rem", color: a.textMuted }}>Totale</Typography>
            <Typography
              sx={{ fontFamily: a.headingFontFamily, fontSize: "1.05rem", fontWeight: 750, color: a.text, letterSpacing: "-0.01em" }}
            >
              {formatCurrency(total)}
            </Typography>
          </Box>
        </Box>
      </Box>

      <Stack spacing={0.75}>
        {slices.map((slice) => (
          <Stack key={slice.id} direction="row" alignItems="center" spacing={1}>
            <Box sx={{ width: 9, height: 9, borderRadius: "2px", backgroundColor: slice.color, flexShrink: 0 }} />
            <Typography
              sx={{
                flex: 1,
                minWidth: 0,
                fontSize: "0.82rem",
                color: slice.id === "__other__" ? a.textSecondary : a.text,
              }}
              noWrap
            >
              {slice.name}
            </Typography>
            <Typography sx={{ fontSize: "0.82rem", fontWeight: 650, color: a.text, fontVariantNumeric: "tabular-nums" }}>
              {formatCurrency(slice.value)}
            </Typography>
            <Typography
              sx={{ fontSize: "0.75rem", color: a.textMuted, width: 40, textAlign: "right", fontVariantNumeric: "tabular-nums" }}
            >
              {formatPercent(slice.share, 0)}
            </Typography>
          </Stack>
        ))}
      </Stack>
    </Stack>
  );
}
