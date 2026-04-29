export interface TransactionQueryRequest {
  fromDate?: string;
  toDate?: string;
  idAccounts?: number[] | null;
  idCategories?: number[] | null;
  includeMoneyTransfers?: boolean;
  page: number;
  pageSize: number;
}
