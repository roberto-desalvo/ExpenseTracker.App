import { AppTokens } from "../../core/theme/theme.service";
import { formatCompact, formatPeriodLabel } from "../../core/utils/format.utilities";

/** Riga della vista tabellare/tooltip: periodo + valori per chiave di serie. */
export type PeriodRow = Record<string, number | string> & { period: string };

/** Unisce più serie per periodo in righe ordinate cronologicamente. */
export const mergeByPeriod = (series: { key: string; values: { period: string; amount: number }[] }[]): PeriodRow[] => {
  const map = new Map<string, PeriodRow>();
  for (const s of series) {
    for (const p of s.values) {
      const row = map.get(p.period) ?? { period: p.period };
      row[s.key] = p.amount;
      map.set(p.period, row);
    }
  }
  return [...map.values()].sort((x, y) => x.period.localeCompare(y.period));
};

export const categoryAxis = (a: AppTokens, periods: string[], showLine = true) => ({
  type: "category" as const,
  data: periods,
  boundaryGap: true,
  axisTick: { show: false },
  axisLine: { show: showLine, lineStyle: { color: a.chart.grid } },
  axisLabel: {
    color: a.chart.axis,
    fontSize: 11,
    margin: 10,
    hideOverlap: true,
    fontFamily: a.fontFamily,
    formatter: (value: string) => formatPeriodLabel(value),
  },
});

export const valueAxis = (a: AppTokens, extra: Record<string, unknown> = {}) => ({
  type: "value" as const,
  splitLine: { lineStyle: { color: a.chart.grid } },
  axisLabel: {
    color: a.chart.axis,
    fontSize: 11,
    fontFamily: a.fontFamily,
    formatter: (value: number) => formatCompact(value),
  },
  ...extra,
});

export const chartGrid = { top: 8, right: 8, bottom: 26, left: 56 };

/** Sfumatura verticale per le aree. */
export const verticalGradient = (color: string, topOpacity: number) => ({
  type: "linear" as const,
  x: 0,
  y: 0,
  x2: 0,
  y2: 1,
  colorStops: [
    { offset: 0, color: withOpacity(color, topOpacity) },
    { offset: 1, color: withOpacity(color, 0) },
  ],
});

/** Applica un'opacità a un colore esadecimale o rgb(a). */
export const withOpacity = (color: string, opacity: number) => {
  const hex = /^#([0-9a-f]{6})$/i.exec(color);
  if (hex) {
    const n = parseInt(hex[1], 16);
    return `rgba(${(n >> 16) & 255}, ${(n >> 8) & 255}, ${n & 255}, ${opacity})`;
  }
  return color;
};

/** Parametri del formatter del tooltip ECharts (sottoinsieme usato). */
export interface TooltipParam {
  dataIndex: number;
  seriesId?: string;
  seriesName?: string;
  value: unknown;
  axisValue?: string;
}

export const asParams = (params: unknown): TooltipParam[] => (Array.isArray(params) ? params : [params]) as TooltipParam[];
