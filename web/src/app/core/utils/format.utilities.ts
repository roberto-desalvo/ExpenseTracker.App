const currencyFormatter = new Intl.NumberFormat("it-IT", {
  style: "currency",
  currency: "EUR",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
  // it-IT non raggruppa i numeri a 4 cifre: forziamo 1.234,56 € per coerenza con 12.345,67 €
  useGrouping: "always" as unknown as boolean,
});

const compactFormatter = new Intl.NumberFormat("it-IT", {
  notation: "compact",
  maximumFractionDigits: 1,
});

/** 1.234,56 € */
export const formatCurrency = (value: number) => currencyFormatter.format(value);

/** +1.234,56 € / −1.234,56 € */
export const formatSignedCurrency = (value: number) =>
  `${value > 0 ? "+" : value < 0 ? "−" : ""}${currencyFormatter.format(Math.abs(value))}`;

/** 1,2 Mln / 12,3 Mila: per tick d'asse */
export const formatCompact = (value: number) => compactFormatter.format(value);

export const formatPercent = (value: number, fractionDigits = 1) =>
  `${value.toLocaleString("it-IT", {
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  })}%`;

const MONTHS_SHORT = ["gen", "feb", "mar", "apr", "mag", "giu", "lug", "ago", "set", "ott", "nov", "dic"];
const MONTHS_LONG = [
  "gennaio",
  "febbraio",
  "marzo",
  "aprile",
  "maggio",
  "giugno",
  "luglio",
  "agosto",
  "settembre",
  "ottobre",
  "novembre",
  "dicembre",
];

/**
 * Converte una chiave di periodo del BE (yyyy-MM-dd, yyyy-Www, yyyy-MM, yyyy)
 * in un'etichetta breve leggibile.
 */
export const formatPeriodLabel = (period: string, long = false): string => {
  if (/^\d{4}-\d{2}-\d{2}$/.test(period)) {
    const [, m, d] = period.split("-");
    return `${Number(d)} ${MONTHS_SHORT[Number(m) - 1]}`;
  }

  if (/^\d{4}-W\d{2}$/.test(period)) {
    const [y, w] = period.split("-W");
    return long ? `Settimana ${Number(w)} ${y}` : `S${Number(w)} '${y.slice(2)}`;
  }

  if (/^\d{4}-\d{2}$/.test(period)) {
    const [y, m] = period.split("-");
    return long ? `${MONTHS_LONG[Number(m) - 1]} ${y}` : `${MONTHS_SHORT[Number(m) - 1]} '${y.slice(2)}`;
  }

  return period;
};

export const formatMonthYear = (date: Date) => `${MONTHS_LONG[date.getMonth()]} ${date.getFullYear()}`;

/** yyyy-MM per la data indicata */
export const toMonthKey = (date: Date) =>
  `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}`;

/** Chiavi yyyy-MM dal mese di `start` al mese di `end` inclusi. */
export const enumerateMonthKeys = (start: Date, end: Date): string[] => {
  const keys: string[] = [];
  const cursor = new Date(start.getFullYear(), start.getMonth(), 1);
  const last = new Date(end.getFullYear(), end.getMonth(), 1);

  while (cursor <= last) {
    keys.push(toMonthKey(cursor));
    cursor.setMonth(cursor.getMonth() + 1);
  }

  return keys;
};

export const initialsOf = (name: string) =>
  name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("") || "?";
