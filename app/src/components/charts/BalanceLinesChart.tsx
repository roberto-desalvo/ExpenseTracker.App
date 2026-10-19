import { useMemo, useState } from "react";
import { Box } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  TooltipProps,
  XAxis,
  YAxis,
} from "recharts";
import { formatCompact, formatCurrency, formatPeriodLabel } from "../../utilities/format.utilities";
import { ChartDataTable, ChartEmpty, ChartLegend, ChartTooltipCard, ChartTooltipRow, ChartView } from "./ChartParts";

export interface LineSeries {
  key: string;
  name: string;
  color: string;
  values: { period: string; amount: number }[];
}

interface BalanceLinesChartProps {
  series: LineSeries[];
  height?: number;
  view?: ChartView;
}

/** Più serie nello stesso asse (saldo per conto). La legenda mostra/nasconde le singole linee. */
export default function BalanceLinesChart({ series, height = 320, view = "chart" }: BalanceLinesChartProps) {
  const a = useTheme().app;
  const [hidden, setHidden] = useState<Set<string>>(new Set());

  const rows = useMemo(() => {
    const map = new Map<string, Record<string, number | string>>();
    for (const s of series) {
      for (const p of s.values) {
        const row = map.get(p.period) ?? { period: p.period };
        row[s.key] = p.amount;
        map.set(p.period, row);
      }
    }
    return [...map.values()].sort((x, y) => String(x.period).localeCompare(String(y.period)));
  }, [series]);

  if (rows.length === 0) {
    return <ChartEmpty height={height} />;
  }

  if (view === "table") {
    return (
      <ChartDataTable
        height={height + 40}
        columns={[{ key: "period", label: "Periodo" }, ...series.map((s) => ({ key: s.key, label: s.name, align: "right" as const }))]}
        rows={[...rows].reverse().map((row) => ({
          period: formatPeriodLabel(String(row.period), true),
          ...Object.fromEntries(series.map((s) => [s.key, typeof row[s.key] === "number" ? formatCurrency(row[s.key] as number) : "—"])),
        }))}
      />
    );
  }

  const toggle = (key: string) =>
    setHidden((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });

  const renderTooltip = ({ active, payload, label }: TooltipProps<number, string>) => {
    if (!active || !payload?.length) return null;
    const sorted = [...payload].sort((x, y) => Number(y.value) - Number(x.value));
    return (
      <ChartTooltipCard title={formatPeriodLabel(String(label), true)}>
        {sorted.map((item) => {
          const s = series.find((x) => x.key === item.dataKey);
          return (
            <ChartTooltipRow
              key={String(item.dataKey)}
              color={s?.color}
              label={s?.name ?? String(item.dataKey)}
              value={formatCurrency(Number(item.value))}
            />
          );
        })}
      </ChartTooltipCard>
    );
  };

  return (
    <Box>
      <ChartLegend
        entries={series.map((s) => ({ key: s.key, label: s.name, color: s.color, hidden: hidden.has(s.key), shape: "line" }))}
        onToggle={toggle}
      />
      <Box sx={{ width: "100%", height, "& .recharts-surface:focus": { outline: "none" } }}>
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={rows} margin={{ top: 4, right: 8, bottom: 0, left: 0 }}>
            <CartesianGrid vertical={false} stroke={a.chart.grid} />
            <XAxis
              dataKey="period"
              tickFormatter={(value: string) => formatPeriodLabel(value)}
              tick={{ fill: a.chart.axis, fontSize: 11 }}
              tickLine={false}
              axisLine={{ stroke: a.chart.grid }}
              minTickGap={20}
              tickMargin={8}
            />
            <YAxis
              tickFormatter={(value: number) => formatCompact(value)}
              tick={{ fill: a.chart.axis, fontSize: 11 }}
              tickLine={false}
              axisLine={false}
              width={52}
            />
            <Tooltip content={renderTooltip} cursor={{ stroke: a.borderStrong, strokeWidth: 1 }} />
            {series.map((s) => (
              <Line
                key={s.key}
                type="monotone"
                dataKey={s.key}
                name={s.name}
                stroke={s.color}
                strokeWidth={2}
                dot={false}
                activeDot={{ r: 4.5, strokeWidth: 2, stroke: a.surface }}
                connectNulls
                hide={hidden.has(s.key)}
                isAnimationActive={false}
              />
            ))}
          </LineChart>
        </ResponsiveContainer>
      </Box>
    </Box>
  );
}
