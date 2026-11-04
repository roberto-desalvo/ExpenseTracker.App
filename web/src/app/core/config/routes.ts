export const ROUTES = {
  dashboard: "/",
  transactions: "/movimenti",
  analytics: "/analisi",
  settings: "/impostazioni",
} as const;

/** Rotte che richiedono l'anagrafica di conti e categorie. */
export const DATA_ROUTES: string[] = Object.values(ROUTES);
