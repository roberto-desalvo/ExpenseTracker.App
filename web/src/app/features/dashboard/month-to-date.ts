import { addMonths, startOfMonth, toDateInputValue } from "../../core/utils/date.utilities";

/** Flusso di un giorno (chiave yyyy-MM-dd della serie giornaliera del BE). */
export interface DailyFlow {
  period: string;
  earned: number;
  spent: number;
  invested: number;
}

export interface MonthToDateAverages {
  avgSpent: number;
  avgEarned: number;
  avgInvested: number;
  avgNet: number;
}

/**
 * Medie dei `months` mesi precedenti a `today`, ciascuno considerato dal primo del mese fino allo
 * stesso giorno di `today` (o all'ultimo giorno, se il mese è più corto): così il confronto con il
 * mese in corso è a parità di giorni trascorsi. I mesi senza movimenti contano come zero.
 */
export const monthToDateAverages = (daily: DailyFlow[], today: Date, months = 6): MonthToDateAverages => {
  const totals = Array.from({ length: months }, (_, i) => {
    const from = toDateInputValue(startOfMonth(today, -(i + 1)));
    const to = toDateInputValue(addMonths(today, -(i + 1)));
    return daily
      .filter((d) => d.period >= from && d.period <= to)
      .reduce(
        (acc, d) => ({ earned: acc.earned + d.earned, spent: acc.spent + d.spent, invested: acc.invested + d.invested }),
        { earned: 0, spent: 0, invested: 0 },
      );
  });
  const avg = (pick: (t: (typeof totals)[number]) => number) =>
    months > 0 ? totals.reduce((s, t) => s + pick(t), 0) / months : 0;
  return {
    avgEarned: avg((t) => t.earned),
    avgSpent: avg((t) => t.spent),
    avgInvested: avg((t) => t.invested),
    avgNet: avg((t) => t.earned - t.spent),
  };
};
