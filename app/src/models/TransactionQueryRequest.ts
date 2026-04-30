export interface TransactionQueryRequest {
  fromDate?: string;
  toDate?: string;
  idAccounts?: number[] | null;
  idCategories?: number[] | null;
  isIncome?: boolean | null;
  page: number;
  pageSize: number;
}
