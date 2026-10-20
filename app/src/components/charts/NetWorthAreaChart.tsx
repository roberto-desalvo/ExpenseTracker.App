import { useId, useMemo, useState } from "react";
import { Box } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import {
  Area,
  CartesianGrid,
  ComposedChart,
  Line,
  ResponsiveContainer,
  Tooltip,
  TooltipProps,
  XAxis,
  YAxis,
} from "recharts";
import { formatCompact, formatCurrency, formatPeriodLabel } from "../../utilities/format.utilities";
import { ChartDataTable, ChartEmpty, ChartLegend, ChartTooltipCard, ChartTooltipRow, ChartView } from "./ChartParts";
import type { LineSeries } from "./BalanceLinesChart";

export interface AmountPoint {
  period: string;
  amount: number;
}

interface NetWorthAreaChartProps {
  points: AmountPoint[];
  height?: number;
  /** "spark": senza assi, per la card hero; "full": con assi e griglia */
  variant?: "spark" | "full";
  color?: string;
  label?: string;
  /** serie per conto sovrapponibili (solo "full"): nascoste di default, si attivano dalla legenda */
  accountSeries?: LineSeries[];
  /** solo "full": in tabella le colonne seguono i conti attivati nella legenda */
  view?: ChartView;
}

const TOTAL_KEY = "amount";
const NO_SERIES: LineSeries[] = [];

/** Andamento del patrimonio: area con linea sottile, con eventuali linee per conto attivabili dalla legenda. */
export default function NetWorthAreaChart({
  points,
  height = 300,
  variant = "full",
  color,
  label = "Patrimonio",
  accountSeries = NO_SERIES,
  view = "chart",
}: NetWorthAreaChartProps) {
  const a = useTheme().app;
  const gradientId = useId().replace(/:/g, "");
  const stroke = color ?? a.chart.series[0];
  const isSpark = variant === "spark";
  const series = isSpark ? NO_SERIES : accountSeries;
  // i conti partono nascosti: si tiene l'insieme di quelli mostrati
  const [shown, setShown] = useState<Set<string>>(new Set());

  const rows = useMemo(() => {
    const map = new Map<string, Record<string, number | string>>();
    for (const p of points) map.set(p.period, { period: p.period, [TOTAL_KEY]: p.amount });
    for (const s of series) {
      for (const p of s.values) {
        const row = map.get(p.period) ?? { period: p.period };
        row[s.key] = p.amount;
        map.set(p.period, row);
      }
    }
    return [...map.values()].sort((x, y) => String(x.period).localeCompare(String(y.period)));
  }, [points, series]);

  if (points.length === 0) {
    return isSpark ? <Box sx={{ height }} /> : <ChartEmpty height={height} />;
  }

  const visibleSeries = series.filter((s) => shown.has(s.key));
  const values = [
    ...points.map((p) => p.amount),
    ...visibleSeries.flatMap((s) => s.values.map((v) => v.amount)),
  ];
  const min = Math.min(...values);
  const max = Math.max(...values);
  const padding = Math.max((max - min) * 0.12, 1);

  const toggle = (key: string) =>
    setShown((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });

  const legend = series.length > 0 && (
    <ChartLegend
      entries={[
        { key: TOTAL_KEY, label, color: stroke, shape: "square" },
        ...series.map((s) => ({ key: s.key, label: s.name, color: s.color, hidden: !shown.has(s.key), shape: "line" as const })),
      ]}
      onToggle={(key) => key !== TOTAL_KEY && toggle(key)}
    />
  );

  if (!isSpark && view === "table") {
    const columns = [{ key: TOTAL_KEY, name: label }, ...visibleSeries];
    return (
      <Box>
        {legend}
        <ChartDataTable
          height={height + 40}
          columns={[{ key: "period", label: "Periodo" }, ...columns.map((c) => ({ key: c.key, label: c.name, align: "right" as const }))]}
          rows={[...rows].reverse().map((row) => ({
            period: formatPeriodLabel(String(row.period), true),
            ...Object.fromEntries(
              columns.map((c) => [c.key, typeof row[c.key] === "number" ? formatCurrency(row[c.key] as number) : "—"]),
            ),
          }))}
        />
      </Box>
    );
  }

  const renderTooltip = ({ active, payload }: TooltipProps<number, string>) => {
    if (!active || !payload?.length) return null;
    const row = payload[0].payload as Record<string, number | string>;
    const accounts = visibleSeries
      .filter((s) => typeof row[s.key] === "number")
      .sort((x, y) => Number(row[y.key]) - Number(row[x.key]));
    return (
      <ChartTooltipCard title={formatPeriodLabel(String(row.period), true)}>
        {typeof row[TOTAL_KEY] === "number" && (
          <ChartTooltipRow color={stroke} label={label} value={formatCurrency(Number(row[TOTAL_KEY]))} emphasis />
        )}
        {accounts.map((s) => (
          <ChartTooltipRow key={s.key} color={s.color} label={s.name} value={formatCurrency(Number(row[s.key]))} />
        ))}
      </ChartTooltipCard>
    );
  };

  return (
    <Box>
      {legend}
      <Box sx={{ width: "100%", height, "& .recharts-surface:focus": { outline: "none" } }}>
        <ResponsiveContainer width="100%" height="100%">
          <ComposedChart data={rows} margin={isSpark ? { top: 6, right: 2, bottom: 2, left: 2 } : { top: 8, right: 8, bottom: 0, left: 0 }}>
            <defs>
              <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor={stroke} stopOpacity={isSpark ? 0.35 : 0.22} />
                <stop offset="100%" stopColor={stroke} stopOpacity={0} />
              </linearGradient>
            </defs>
            {!isSpark && <CartesianGrid vertical={false} stroke={a.chart.grid} />}
            <XAxis
              dataKey="period"
              hide={isSpark}
              tickFormatter={(value: string) => formatPeriodLabel(value)}
              tick={{ fill: a.chart.axis, fontSize: 11 }}
              tickLine={false}
              axisLine={{ stroke: a.chart.grid }}
              minTickGap={24}
              tickMargin={8}
            />
            <YAxis
              hide={isSpark}
              domain={[min - padding, max + padding]}
              tickFormatter={(value: number) => formatCompact(value)}
              tick={{ fill: a.chart.axis, fontSize: 11 }}
              tickLine={false}
              axisLine={false}
              width={52}
            />
            <Tooltip
              content={renderTooltip}
              cursor={{ stroke: isSpark ? "rgba(255,255,255,0.5)" : a.borderStrong, strokeWidth: 1 }}
            />
            <Area
              type="monotone"
              dataKey={TOTAL_KEY}
              stroke={stroke}
              strokeWidth={2}
              fill={`url(#${gradientId})`}
              dot={false}
              activeDot={{ r: 4.5, strokeWidth: 2, stroke: isSpark ? "transparent" : a.surface, fill: stroke }}
              connectNulls
              isAnimationActive={false}
            />
            {series.map((s) => (
              <Line
                key={s.key}
                type="monotone"
                dataKey={s.key}
                name={s.name}
                stroke={s.color}
                strokeWidth={1.75}
                dot={false}
                activeDot={{ r: 4, strokeWidth: 2, stroke: a.surface }}
                connectNulls
                hide={!shown.has(s.key)}
                isAnimationActive={false}
              />
            ))}
          </ComposedChart>
        </ResponsiveContainer>
      </Box>
    </Box>
  );
}
