import { useId } from "react";
import { Box } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  TooltipProps,
  XAxis,
  YAxis,
} from "recharts";
import { formatCompact, formatCurrency, formatPeriodLabel } from "../../utilities/format.utilities";
import { ChartEmpty, ChartTooltipCard, ChartTooltipRow } from "./ChartParts";

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
}

/** Andamento di una sola grandezza nel tempo (patrimonio): area con linea sottile. */
export default function NetWorthAreaChart({
  points,
  height = 300,
  variant = "full",
  color,
  label = "Patrimonio",
}: NetWorthAreaChartProps) {
  const a = useTheme().app;
  const gradientId = useId().replace(/:/g, "");
  const stroke = color ?? a.chart.series[0];
  const isSpark = variant === "spark";

  if (points.length === 0) {
    return isSpark ? <Box sx={{ height }} /> : <ChartEmpty height={height} />;
  }

  const values = points.map((p) => p.amount);
  const min = Math.min(...values);
  const max = Math.max(...values);
  const padding = Math.max((max - min) * 0.12, 1);

  const renderTooltip = ({ active, payload }: TooltipProps<number, string>) => {
    if (!active || !payload?.length) return null;
    const point = payload[0].payload as AmountPoint;
    return (
      <ChartTooltipCard title={formatPeriodLabel(point.period, true)}>
        <ChartTooltipRow color={stroke} label={label} value={formatCurrency(point.amount)} emphasis />
      </ChartTooltipCard>
    );
  };

  return (
    <Box sx={{ width: "100%", height, "& .recharts-surface:focus": { outline: "none" } }}>
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={points} margin={isSpark ? { top: 6, right: 2, bottom: 2, left: 2 } : { top: 8, right: 8, bottom: 0, left: 0 }}>
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
            dataKey="amount"
            stroke={stroke}
            strokeWidth={2}
            fill={`url(#${gradientId})`}
            dot={false}
            activeDot={{ r: 4.5, strokeWidth: 2, stroke: isSpark ? "transparent" : a.surface, fill: stroke }}
            isAnimationActive={false}
          />
        </AreaChart>
      </ResponsiveContainer>
    </Box>
  );
}
