export type ImportStatus = "Queued" | "Processing" | "Completed" | "Failed";

/** Un import di file (dall'app, da OneDrive o dagli script), con il suo stato e l'esito. */
export interface ImportLog {
  id: number;
  importedOn: string;
  status: ImportStatus;
  startedOn?: string | null;
  completedOn?: string | null;
  source: string;
  fileName?: string | null;
  accountId?: number | null;
  accountName?: string | null;
  minTransactionDate?: string | null;
  maxTransactionDate?: string | null;
  newTransactionsCount: number;
  newTransfersCount: number;
  skippedDuplicatesCount: number;
  errorMessage?: string | null;
}

export interface ImportLogQueryRequest {
  page: number;
  pageSize: number;
  statuses?: ImportStatus[];
}

export const isImportActive = (log: Pick<ImportLog, "status">) => log.status === "Queued" || log.status === "Processing";
