export const ROUTES = {
  dashboard: "/",
  transactions: "/movimenti",
  imports: "/movimenti/import",
  analytics: "/analisi",
  settings: "/impostazioni",
} as const;

/** Rotte che richiedono l'anagrafica di conti e categorie. */
export const DATA_ROUTES: string[] = Object.values(ROUTES);
