import { useMemo } from "react";
import { Box } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import {
  Bar,
  CartesianGrid,
  ComposedChart,
  Line,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  TooltipProps,
  XAxis,
  YAxis,
} from "recharts";
import {
  formatCompact,
  formatCurrency,
  formatPeriodLabel,
  formatSignedCurrency,
} from "../../utilities/format.utilities";
import { ChartDataTable, ChartEmpty, ChartLegend, ChartTooltipCard, ChartTooltipRow, ChartView } from "./ChartParts";

export interface CashFlowPoint {
  period: string;
  earned: number;
  spent: number;
}

interface CashFlowChartProps {
  points: CashFlowPoint[];
  height?: number;
  view?: ChartView;
}

interface Row extends CashFlowPoint {
  spentNeg: number;
  net: number;
}

/**
 * Flusso di cassa per periodo: entrate sopra lo zero, uscite sotto, saldo netto come linea.
 * Un solo asse (euro) per tutte e tre le grandezze.
 */
export default function CashFlowChart({ points, height = 300, view = "chart" }: CashFlowChartProps) {
  const a = useTheme().app;

  const rows = useMemo<Row[]>(
    () => points.map((p) => ({ ...p, spentNeg: -p.spent, net: p.earned - p.spent })),
    [points],
  );

  if (rows.length === 0 || rows.every((r) => r.earned === 0 && r.spent === 0)) {
    return <ChartEmpty height={height} />;
  }

  if (view === "table") {
    return (
      <ChartDataTable
        height={height + 34}
        columns={[
          { key: "period", label: "Periodo" },
          { key: "earned", label: "Entrate", align: "right" },
          { key: "spent", label: "Uscite", align: "right" },
          { key: "net", label: "Saldo", align: "right" },
        ]}
        rows={[...rows].reverse().map((r) => ({
          period: formatPeriodLabel(r.period, true),
          earned: formatCurrency(r.earned),
          spent: formatCurrency(r.spent),
          net: formatSignedCurrency(r.net),
        }))}
      />
    );
  }

  const renderTooltip = ({ active, payload }: TooltipProps<number, string>) => {
    if (!active || !payload?.length) return null;
    const row = payload[0].payload as Row;
    return (
      <ChartTooltipCard title={formatPeriodLabel(row.period, true)}>
        <ChartTooltipRow color={a.income} label="Entrate" value={formatCurrency(row.earned)} />
        <ChartTooltipRow color={a.expense} label="Uscite" value={formatCurrency(row.spent)} />
        <ChartTooltipRow color={a.chart.net} label="Saldo" value={formatSignedCurrency(row.net)} emphasis />
      </ChartTooltipCard>
    );
  };

  return (
    <Box>
      <ChartLegend
        entries={[
          { key: "earned", label: "Entrate", color: a.income },
          { key: "spent", label: "Uscite", color: a.expense },
          { key: "net", label: "Saldo netto", color: a.chart.net, shape: "line" },
        ]}
      />
      <Box sx={{ width: "100%", height, "& .recharts-surface:focus": { outline: "none" } }}>
        <ResponsiveContainer width="100%" height="100%">
          <ComposedChart data={rows} stackOffset="sign" margin={{ top: 4, right: 8, bottom: 0, left: 0 }} barCategoryGap="28%">
            <CartesianGrid vertical={false} stroke={a.chart.grid} />
            <XAxis
              dataKey="period"
              tickFormatter={(value: string) => formatPeriodLabel(value)}
              tick={{ fill: a.chart.axis, fontSize: 11 }}
              tickLine={false}
              axisLine={false}
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
            <ReferenceLine y={0} stroke={a.borderStrong} />
            <Tooltip content={renderTooltip} cursor={{ fill: a.surfaceAlt, opacity: 0.7 }} />
            <Bar dataKey="earned" stackId="flow" fill={a.income} radius={[4, 4, 0, 0]} maxBarSize={36} isAnimationActive={false} />
            <Bar dataKey="spentNeg" stackId="flow" fill={a.expense} radius={[4, 4, 0, 0]} maxBarSize={36} isAnimationActive={false} />
            <Line
              type="monotone"
              dataKey="net"
              stroke={a.chart.net}
              strokeWidth={2}
              dot={{ r: 3, strokeWidth: 2, stroke: a.surface, fill: a.chart.net }}
              activeDot={{ r: 5, strokeWidth: 2, stroke: a.surface }}
              isAnimationActive={false}
            />
          </ComposedChart>
        </ResponsiveContainer>
      </Box>
    </Box>
  );
}
