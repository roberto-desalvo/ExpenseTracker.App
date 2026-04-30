export interface TransactionQueryRequest {
  fromDate?: string;
  toDate?: string;
  idAccounts?: number[] | null;
  idCategories?: number[] | null;
  page: number;
  pageSize: number;
}
