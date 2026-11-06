export interface TimeSeriesDimension {
  key: string;
  value: string;
}

export interface TimeSeriesPoint {
  period: string;
  amount: number;
  earned: number;
  spent: number;
  /** parte di `spent` nella categoria investimenti */
  invested: number;
}

export interface TimeSeries {
  dimensions: TimeSeriesDimension[];
  values: TimeSeriesPoint[];
  /** solo serie di saldo: saldo puntuale prima dell'inizio e alla fine dell'intervallo */
  openingBalance?: number | null;
  closingBalance?: number | null;
}

export interface TimeSeriesList {
  granularity: string;
  series: TimeSeries[];
}
