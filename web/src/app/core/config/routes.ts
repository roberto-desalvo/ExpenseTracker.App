export const ROUTES = {
  dashboard: "/",
  transactions: "/movimenti",
  imports: "/movimenti/import",
  legacyMatch: "/movimenti/abbinamento-excel",
  analytics: "/analisi",
  planning: "/pianificazione",
  settings: "/impostazioni",
} as const;

/** Rotte che richiedono l'anagrafica di conti e categorie. */
export const DATA_ROUTES: string[] = Object.values(ROUTES);
