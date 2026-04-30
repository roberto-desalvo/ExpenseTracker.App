export interface TimeSeriesRequest {
  startDate: string;
  endDate: string;
  idAccounts: number[];
  idCategories: number[];
  granularity: number;
}
