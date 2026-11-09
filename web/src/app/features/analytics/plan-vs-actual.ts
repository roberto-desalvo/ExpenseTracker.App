import { toDateInputValue } from "../../core/utils/date.utilities";

/** 1 giorno, 2 settimana ISO, 3 mese, 4 anno (come la granularità delle serie del BE). */
export type PlanGranularity = 1 | 2 | 3 | 4;

export interface PlanVsActualPoint {
  period: string;
  planned: number;
  spent: number;
}

const pad = (value: number) => String(value).padStart(2, "0");

/** Chiave di periodo nello stesso formato del BE (yyyy-MM-dd, yyyy-Www, yyyy-MM, yyyy). */
export const periodKeyOf = (date: Date, granularity: PlanGranularity): string => {
  switch (granularity) {
    case 1:
      return toDateInputValue(date);
    case 2: {
      // settimana ISO: quella del giovedì della stessa settimana (lunedì–domenica)
      const thursday = new Date(date.getFullYear(), date.getMonth(), date.getDate() + 3 - ((date.getDay() + 6) % 7));
      const jan1 = new Date(thursday.getFullYear(), 0, 1);
      const week = Math.floor(Math.round((thursday.getTime() - jan1.getTime()) / 86_400_000) / 7) + 1;
      return `${thursday.getFullYear()}-W${pad(week)}`;
    }
    case 3:
      return `${date.getFullYear()}-${pad(date.getMonth() + 1)}`;
    default:
      return String(date.getFullYear());
  }
};

/**
 * Previsto ed effettivo per periodo nell'intervallo [start, end] (yyyy-MM-dd). La previsione mensile è divisa in
 * parti uguali sui giorni del mese, così i periodi più piccoli del mese (o i mesi tagliati dall'intervallo) ricevono
 * la quota dei loro giorni. Si confrontano solo i giorni dei mesi con una previsione: lo speso dei mesi non
 * pianificati non entra nel confronto e i periodi senza alcun giorno pianificato sono omessi.
 */
export const planVsActual = (
  start: string,
  end: string,
  granularity: PlanGranularity,
  plannedByMonth: ReadonlyMap<string, number>,
  spentByDay: ReadonlyMap<string, number>,
): PlanVsActualPoint[] => {
  const points = new Map<string, PlanVsActualPoint>();
  const last = new Date(`${end}T00:00:00`);
  for (let day = new Date(`${start}T00:00:00`); day <= last; day = new Date(day.getFullYear(), day.getMonth(), day.getDate() + 1)) {
    const monthPlanned = plannedByMonth.get(periodKeyOf(day, 3)) ?? 0;
    if (monthPlanned <= 0) continue;
    const daysInMonth = new Date(day.getFullYear(), day.getMonth() + 1, 0).getDate();
    const key = periodKeyOf(day, granularity);
    const point = points.get(key) ?? { period: key, planned: 0, spent: 0 };
    point.planned += monthPlanned / daysInMonth;
    point.spent += spentByDay.get(toDateInputValue(day)) ?? 0;
    points.set(key, point);
  }
  return [...points.values()];
};
