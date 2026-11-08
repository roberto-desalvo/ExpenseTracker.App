import { TransactionMonthOption } from "../../core/models/TransactionMonthOption";
import { startOfMonth, toDateInputValue } from "../../core/utils/date.utilities";
import { formatMonthYear } from "../../core/utils/format.utilities";

/** Mesi navigabili nella pianificazione: da `back` mesi fa a `ahead` mesi avanti, dal più recente. */
export const planningMonths = (today: Date, back = 24, ahead = 12): TransactionMonthOption[] => {
  const months: TransactionMonthOption[] = [];
  for (let offset = ahead; offset >= -back; offset--) {
    const start = startOfMonth(today, offset);
    months.push({
      startDate: toDateInputValue(start),
      endDate: toDateInputValue(new Date(start.getFullYear(), start.getMonth() + 1, 0)),
      description: formatMonthYear(start),
    });
  }
  return months;
};

/** Importo digitato (virgola o punto decimale); null se vuoto, NaN se non valido. */
export const parseAmount = (text: string | null | undefined): number | null => {
  const trimmed = (text ?? "").trim().replace(/\s|€/g, "");
  if (!trimmed) return null;
  // 1.234,56 → 1234.56; 12.5 → 12.5
  const normalized = trimmed.includes(",") ? trimmed.replace(/\./g, "").replace(",", ".") : trimmed;
  return /^\d+(\.\d+)?$/.test(normalized) ? Number(normalized) : NaN;
};

/** Valore iniziale del campo importo. */
export const amountText = (value: number | null | undefined) =>
  value === null || value === undefined ? "" : String(value).replace(".", ",");

export interface BudgetProgress {
  /** speso / previsto (1 = 100%) */
  ratio: number;
  /** larghezza della barra, 0–100 */
  fill: number;
  /** speso − previsto: positivo = sforamento */
  diff: number;
  over: boolean;
}

/** Avanzamento della spesa rispetto alla previsione; null senza previsione o senza spesa effettiva. */
export const budgetProgress = (planned: number | null, spent: number | null): BudgetProgress | null => {
  if (planned === null || planned <= 0 || spent === null) return null;
  const ratio = spent / planned;
  return { ratio, fill: Math.min(ratio, 1) * 100, diff: spent - planned, over: spent > planned };
};
