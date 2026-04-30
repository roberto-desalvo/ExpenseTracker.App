import { Box, Typography, useTheme } from "@mui/material";
import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

export interface TimeSeriesLineChartSeries {
  name: string;
  values: {
    period: string;
    amount: number;
  }[];
}

interface TimeSeriesLineChartProps {
  series: TimeSeriesLineChartSeries[];
  height?: number;
  emptyMessage?: string;
}

const COLORS = [
  "#4f46e5",
  "#0891b2",
  "#16a34a",
  "#d97706",
  "#db2777",
  "#dc2626",
  "#7c3aed",
  "#0f766e",
  "#334155",
];

const formatAmount = (value: number) =>
  value.toLocaleString("it-IT", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

const buildChartData = (series: TimeSeriesLineChartSeries[]) => {
  const periodMap = new Map<string, Record<string, number | string>>();

  for (const serie of series) {
    for (const point of serie.values) {
      const current = periodMap.get(point.period) ?? { period: point.period };
      current[serie.name] = point.amount;
      periodMap.set(point.period, current);
    }
  }

  return Array.from(periodMap.values()).sort((a, b) =>
    String(a.period).localeCompare(String(b.period)),
  );
};

export default function TimeSeriesLineChart({
  series,
  height = 380,
  emptyMessage = "Nessun dato disponibile",
}: TimeSeriesLineChartProps) {
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

  const data = buildChartData(series);

  return (
    <Box sx={{ width: "100%", height }}>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart
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
          <Legend />
          {series.map((serie, index) => (
            <Line
              key={serie.name}
              type="monotone"
              dataKey={serie.name}
              stroke={COLORS[index % COLORS.length]}
              strokeWidth={2}
              dot={false}
              connectNulls
            />
          ))}
        </LineChart>
      </ResponsiveContainer>
    </Box>
  );
}
