export const toIsoDateStart = (value: string): string =>
  new Date(`${value}T00:00:00`).toISOString();

export const toIsoDateEnd = (value: string): string =>
  new Date(`${value}T23:59:59.999`).toISOString();
