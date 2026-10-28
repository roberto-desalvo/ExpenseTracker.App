import { useMemo } from "react";
import { Box } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  TooltipProps,
  XAxis,
  YAxis,
} from "recharts";
import { formatCompact, formatCurrency, formatPeriodLabel } from "../../utilities/format.utilities";
import { ChartDataTable, ChartEmpty, ChartLegend, ChartTooltipCard, ChartTooltipRow, ChartView } from "./ChartParts";

export interface StackSeries {
  key: string;
  name: string;
  values: { period: string; value: number }[];
}

interface StackedPeriodBarsProps {
  series: StackSeries[];
  /** serie mostrate con un proprio colore (inclusa "Altre"); le altre confluiscono in "Altre"; `Infinity` mostra tutte le serie */
  maxSeries?: number;
  height?: number;
  view?: ChartView;
}

const OTHER_KEY = "__other__";

/**
 * Barre impilate per periodo (composizione delle spese per categoria nel tempo).
 * Le serie sono ordinate per totale e oltre `maxSeries` raggruppate in "Altre".
 * Oltre la palette i colori ripartono con opacità ridotta, così ogni serie resta
 * distinguibile dalle vicine.
 */
export default function StackedPeriodBars({ series, maxSeries = 6, height = 320, view = "chart" }: StackedPeriodBarsProps) {
  const a = useTheme().app;

  const { rows, keys } = useMemo(() => {
    const ranked = series
      .map((s) => ({ ...s, total: s.values.reduce((sum, p) => sum + p.value, 0) }))
      .filter((s) => s.total > 0)
      .sort((x, y) => y.total - x.total);

    const head = ranked.length > maxSeries ? ranked.slice(0, maxSeries - 1) : ranked;
    const tail = ranked.slice(head.length);

    const map = new Map<string, Record<string, number | string>>();
    const add = (period: string, key: string, value: number) => {
      const row = map.get(period) ?? { period };
      row[key] = (Number(row[key]) || 0) + value;
      map.set(period, row);
    };

    head.forEach((s) => s.values.forEach((p) => add(p.period, s.key, p.value)));
    tail.forEach((s) => s.values.forEach((p) => add(p.period, OTHER_KEY, p.value)));

    const palette = a.chart.series;
    const seriesKeys = [
      ...head.map((s, i) => ({
        key: s.key,
        name: s.name,
        color: palette.length ? palette[i % palette.length] : a.chart.other,
        opacity: Math.max(0.3, 1 - (palette.length ? Math.floor(i / palette.length) : 0) * 0.4),
      })),
      ...(tail.length ? [{ key: OTHER_KEY, name: `Altre (${tail.length})`, color: a.chart.other, opacity: 1 }] : []),
    ];

    return {
      rows: [...map.values()].sort((x, y) => String(x.period).localeCompare(String(y.period))),
      keys: seriesKeys,
    };
  }, [series, maxSeries, a.chart.series, a.chart.other]);

  if (rows.length === 0) {
    return <ChartEmpty height={height} />;
  }

  if (view === "table") {
    return (
      <ChartDataTable
        height={height + 40}
        columns={[{ key: "period", label: "Periodo" }, ...keys.map((k) => ({ key: k.key, label: k.name, align: "right" as const }))]}
        rows={[...rows].reverse().map((row) => ({
          period: formatPeriodLabel(String(row.period), true),
          ...Object.fromEntries(keys.map((k) => [k.key, formatCurrency(Number(row[k.key]) || 0)])),
        }))}
      />
    );
  }

  const renderTooltip = ({ active, payload, label }: TooltipProps<number, string>) => {
    if (!active || !payload?.length) return null;
    const total = payload.reduce((sum, item) => sum + (Number(item.value) || 0), 0);
    return (
      <ChartTooltipCard title={formatPeriodLabel(String(label), true)}>
        {[...payload].reverse().map((item) => {
          const k = keys.find((x) => x.key === item.dataKey);
          return (
            <ChartTooltipRow
              key={String(item.dataKey)}
              color={k?.color}
              opacity={k?.opacity}
              label={k?.name ?? String(item.dataKey)}
              value={formatCurrency(Number(item.value) || 0)}
            />
          );
        })}
        <ChartTooltipRow label="Totale" value={formatCurrency(total)} emphasis />
      </ChartTooltipCard>
    );
  };

  return (
    <Box>
      <ChartLegend entries={keys.map((k) => ({ key: k.key, label: k.name, color: k.color, opacity: k.opacity }))} />
      <Box sx={{ width: "100%", height, "& .recharts-surface:focus": { outline: "none" } }}>
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={rows} margin={{ top: 4, right: 8, bottom: 0, left: 0 }} barCategoryGap="24%">
            <CartesianGrid vertical={false} stroke={a.chart.grid} />
            <XAxis
              dataKey="period"
              tickFormatter={(value: string) => formatPeriodLabel(value)}
              tick={{ fill: a.chart.axis, fontSize: 11 }}
              tickLine={false}
              axisLine={{ stroke: a.chart.grid }}
              minTickGap={16}
              tickMargin={8}
            />
            <YAxis
              tickFormatter={(value: number) => formatCompact(value)}
              tick={{ fill: a.chart.axis, fontSize: 11 }}
              tickLine={false}
              axisLine={false}
              width={52}
            />
            <Tooltip content={renderTooltip} cursor={{ fill: a.surfaceAlt, opacity: 0.7 }} />
            {keys.map((k, i) => (
              <Bar
                key={k.key}
                dataKey={k.key}
                stackId="stack"
                fill={k.color}
                fillOpacity={k.opacity}
                stroke={a.surface}
                strokeWidth={1}
                maxBarSize={44}
                radius={i === keys.length - 1 ? [4, 4, 0, 0] : 0}
                isAnimationActive={false}
              />
            ))}
          </BarChart>
        </ResponsiveContainer>
      </Box>
    </Box>
  );
}
