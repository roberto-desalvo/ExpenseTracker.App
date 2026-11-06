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
  /** Conto senza import automatico (Contanti, Hype): la riga va creata come nuovo movimento. */
  toCreate: boolean;
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

/** Esito del ripristino di un foglio: movimenti riportati allo stato di import e movimenti creati eliminati. */
export interface LegacyResetResult {
  restored: number;
  deleted: number;
}

export interface LegacyCreateItem {
  accountId: number;
  date: string;
  amount: number;
  description: string;
  categoryId: number | null;
}

export interface LegacyMatchApplyItem {
  transactionId: number;
  description: string;
  categoryId: number | null;
}
