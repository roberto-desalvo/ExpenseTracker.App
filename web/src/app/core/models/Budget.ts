/** Riga di pianificazione: previsione e (mese corrente o passato) spesa effettiva della categoria. */
export interface BudgetLine {
  categoryId: number;
  name: string;
  planned: number | null;
  spent: number | null;
}

export interface BudgetMonth {
  /** primo giorno del mese (ISO) */
  month: string;
  /** mese corrente o futuro */
  isEditable: boolean;
  /** mese corrente (fino a oggi) o passato */
  hasActuals: boolean;
  totalPlanned: number;
  totalSpent: number | null;
  /** spesa in categorie senza previsione o senza categoria */
  unplannedSpent: number | null;
  lines: BudgetLine[];
}

/** Totale previsto di un mese (solo mesi con almeno una previsione). */
export interface BudgetTotal {
  /** primo giorno del mese (ISO) */
  month: string;
  planned: number;
}

export interface BudgetMonthUpdate {
  /** yyyy-MM-01 */
  month: string;
  lines: { categoryId: number; amount: number | null }[];
}
