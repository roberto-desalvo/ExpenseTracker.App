export const toIsoDateStart = (value: string): string =>
  new Date(`${value}T00:00:00`).toISOString();

export const toIsoDateEnd = (value: string): string =>
  new Date(`${value}T23:59:59.999`).toISOString();

/** yyyy-MM-dd nel fuso locale (valore per input type="date"). */
export const toDateInputValue = (date: Date): string =>
  `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;

/** Primo giorno del mese di `date` spostato di `monthsOffset` mesi. */
export const startOfMonth = (date: Date, monthsOffset = 0): Date =>
  new Date(date.getFullYear(), date.getMonth() + monthsOffset, 1);

/** Intervallo di giorni interi, estremi inclusi in formato yyyy-MM-dd. */
export interface PeriodRange {
  from: string;
  to: string;
}

/**
 * Giorni coperti da una chiave di periodo del BE (yyyy-MM-dd, yyyy-Www settimana ISO, yyyy-MM, yyyy);
 * null se la chiave non è riconosciuta.
 */
export const periodRange = (period: string): PeriodRange | null => {
  if (/^\d{4}-\d{2}-\d{2}$/.test(period)) return { from: period, to: period };

  const week = /^(\d{4})-W(\d{2})$/.exec(period);
  if (week) {
    // la settimana ISO 1 è quella che contiene il 4 gennaio; le settimane partono di lunedì
    const jan4 = new Date(Number(week[1]), 0, 4);
    const monday = new Date(jan4.getFullYear(), 0, 4 - ((jan4.getDay() + 6) % 7) + (Number(week[2]) - 1) * 7);
    const sunday = new Date(monday.getFullYear(), monday.getMonth(), monday.getDate() + 6);
    return { from: toDateInputValue(monday), to: toDateInputValue(sunday) };
  }

  const month = /^(\d{4})-(\d{2})$/.exec(period);
  if (month) {
    const first = new Date(Number(month[1]), Number(month[2]) - 1, 1);
    return { from: toDateInputValue(first), to: toDateInputValue(new Date(first.getFullYear(), first.getMonth() + 1, 0)) };
  }

  if (/^\d{4}$/.test(period)) return { from: `${period}-01-01`, to: `${period}-12-31` };

  return null;
};
