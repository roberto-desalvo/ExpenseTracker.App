export type TransactionSortField = "date" | "description" | "category" | "account" | "amount";
export type SortDirection = "asc" | "desc";

export interface TransactionQueryRequest {
  fromDate?: string;
  toDate?: string;
  idAccounts?: number[] | null;
  idCategories?: number[] | null;
  isIncome?: boolean | null;
  /** Testo cercato nella descrizione (parziale, case insensitive). */
  search?: string | null;
  sortBy?: TransactionSortField;
  sortDirection?: SortDirection;
  page: number;
  pageSize: number;
}
