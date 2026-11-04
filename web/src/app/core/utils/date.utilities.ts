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
