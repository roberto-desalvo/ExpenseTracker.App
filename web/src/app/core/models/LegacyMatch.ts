/** Abbinamento dello storico Excel (Spese.xlsx): un foglio per mese. */
export interface LegacySheet {
  name: string;
  year: number;
  month: number;
}

export interface LegacyExcelRow {
  rowNumber: number;
  date: string;
  description: string;
  /** Firmato: uscite negative, entrate positive. */
  amount: number;
  accountName: string | null;
  accountId: number | null;
  excelCategory: string | null;
  suggestedCategoryId: number | null;
}

export interface LegacyDbTransaction {
  id: number;
  date: string | null;
  description: string;
  amount: number;
  accountId: number;
  account: string;
  categoryId: number | null;
  category: string | null;
  isTransfer: boolean;
  legacyMatchedAt: string | null;
}

export interface LegacyMatchSuggestion {
  rowNumber: number;
  transactionId: number;
}

export interface LegacyMonth {
  sheet: LegacySheet;
  excelRows: LegacyExcelRow[];
  dbTransactions: LegacyDbTransaction[];
  suggestions: LegacyMatchSuggestion[];
}

export interface LegacyMatchApplyItem {
  transactionId: number;
  description: string;
  categoryId: number | null;
}
