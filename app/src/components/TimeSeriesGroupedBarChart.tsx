import { Box, Typography, useTheme } from "@mui/material";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

export interface TimeSeriesGroupedBarChartSeries {
  name: string;
  values: {
    period: string;
    earned: number;
    spent: number;
  }[];
}

interface TimeSeriesGroupedBarChartProps {
  series: TimeSeriesGroupedBarChartSeries[];
  height?: number;
  emptyMessage?: string;
}

const EARNED_SUFFIX = "__entrate";
const SPENT_SUFFIX = "__uscite";

const formatAmount = (value: number) =>
  value.toLocaleString("it-IT", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

const buildBarChartData = (series: TimeSeriesGroupedBarChartSeries[]) => {
  const periodMap = new Map<string, Record<string, number | string>>();

  for (const serie of series) {
    for (const point of serie.values) {
      const current = periodMap.get(point.period) ?? { period: point.period };
      current[`${serie.name}${EARNED_SUFFIX}`] = point.earned;
      current[`${serie.name}${SPENT_SUFFIX}`] = point.spent;
      periodMap.set(point.period, current);
    }
  }

  return Array.from(periodMap.values()).sort((a, b) =>
    String(a.period).localeCompare(String(b.period)),
  );
};

export default function TimeSeriesGroupedBarChart({
  series,
  height = 380,
  emptyMessage = "Nessun dato disponibile",
}: TimeSeriesGroupedBarChartProps) {
  const theme = useTheme();
  const c = theme.palette.custom;

  if (series.length === 0 || series.every((serie) => serie.values.length === 0)) {
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

  const data = buildBarChartData(series);
  const legendPayload = [
    { value: "Entrate", type: "square" as const, color: c.amountPositive },
    { value: "Uscite", type: "square" as const, color: c.amountNegative },
  ];

  return (
    <Box sx={{ width: "100%", height }}>
      <ResponsiveContainer width="100%" height="100%">
        <BarChart
          data={data}
          margin={{ top: 16, right: 24, left: 12, bottom: 16 }}
        >
          <CartesianGrid strokeDasharray="3 3" stroke={c.filterBorder} />
          <XAxis dataKey="period" tick={{ fill: c.tableHeaderText, fontSize: 12 }} />
          <YAxis
            tick={{ fill: c.tableHeaderText, fontSize: 12 }}
            tickFormatter={(value: number) => formatAmount(value)}
          />
          <Tooltip
            formatter={(value: number) => formatAmount(value)}
            contentStyle={{
              borderRadius: 10,
              border: `1px solid ${c.filterBorder}`,
            }}
          />
          <Legend payload={legendPayload} />
          {series.map((serie) => (
            <Bar
              key={`${serie.name}${EARNED_SUFFIX}`}
              dataKey={`${serie.name}${EARNED_SUFFIX}`}
              name={`${serie.name} - Entrate`}
              fill={c.amountPositive}
            />
          ))}
          {series.map((serie) => (
            <Bar
              key={`${serie.name}${SPENT_SUFFIX}`}
              dataKey={`${serie.name}${SPENT_SUFFIX}`}
              name={`${serie.name} - Uscite`}
              fill={c.amountNegative}
            />
          ))}
        </BarChart>
      </ResponsiveContainer>
    </Box>
  );
}
