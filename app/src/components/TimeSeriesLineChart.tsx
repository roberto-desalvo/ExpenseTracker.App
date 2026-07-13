import { useEffect, useMemo, useState } from "react";
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
import { CHART_SERIES_COLORS } from "../theme/chartColors";

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
  enableLegendToggle?: boolean;
  tightYAxis?: boolean;
  stepped?: boolean;
}

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
  enableLegendToggle = true,
  tightYAxis = false,
  stepped = false,
}: TimeSeriesLineChartProps) {
  const theme = useTheme();
  const c = theme.palette.custom;
  const [hiddenSeries, setHiddenSeries] = useState<Set<string>>(new Set());

  const seriesNames = useMemo(() => series.map((item) => item.name), [series]);

  useEffect(() => {
    if (!enableLegendToggle) {
      setHiddenSeries(new Set());
      return;
    }

    setHiddenSeries((prev) => {
      const next = new Set<string>();
      prev.forEach((name) => {
        if (seriesNames.includes(name)) {
          next.add(name);
        }
      });
      return next;
    });
  }, [seriesNames, enableLegendToggle]);

  const handleLegendClick = (entry: { dataKey?: string | number | ((obj: unknown) => unknown) }) => {
    if (!enableLegendToggle || !entry?.dataKey || typeof entry.dataKey === "function") {
      return;
    }

    const key = String(entry.dataKey);
    setHiddenSeries((prev) => {
      const next = new Set(prev);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }

      return next;
    });
  };

  const formatLegendLabel = (value: string | number) => {
    const key = String(value);
    const isHidden = hiddenSeries.has(key);

    return (
      <span
        style={{
          opacity: isHidden ? 0.45 : 1,
          textDecoration: isHidden ? "line-through" : "none",
          cursor: enableLegendToggle ? "pointer" : "default",
        }}
      >
        {key}
      </span>
    );
  };

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

  const yAxisDomain: ((
    dataRange: [number, number],
    allowDataOverflow: boolean,
  ) => [number, number]) | undefined = tightYAxis
    ? ([dataMin, dataMax]: [number, number], _allowDataOverflow: boolean) => {
        const range = Math.max(dataMax - dataMin, 1);
        const padding = range * 0.08;
        return [dataMin - padding, dataMax + padding] as [number, number];
      }
    : undefined;

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
            domain={yAxisDomain}
          />
          <Tooltip
            formatter={(value: number) => formatAmount(value)}
            contentStyle={{
              borderRadius: 10,
              border: `1px solid ${c.filterBorder}`,
            }}
          />
          <Legend
            onClick={enableLegendToggle ? handleLegendClick : undefined}
            formatter={formatLegendLabel}
          />
          {series.map((serie, index) => (
            <Line
              key={serie.name}
              type={stepped ? "stepAfter" : "monotone"}
              dataKey={serie.name}
              stroke={CHART_SERIES_COLORS[index % CHART_SERIES_COLORS.length]}
              strokeWidth={2}
              dot={false}
              connectNulls
              hide={enableLegendToggle && hiddenSeries.has(serie.name)}
            />
          ))}
        </LineChart>
      </ResponsiveContainer>
    </Box>
  );
}
