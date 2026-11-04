import { ROUTES } from "./routes";

export interface NavItem {
  label: string;
  path: string;
  /** nome dell'icona Material Symbols */
  icon: string;
  description: string;
}

export const NAV_ITEMS: NavItem[] = [
  { label: "Dashboard", path: ROUTES.dashboard, icon: "space_dashboard", description: "Panoramica del mese e del patrimonio" },
  { label: "Movimenti", path: ROUTES.transactions, icon: "receipt_long", description: "Elenco transazioni con filtri" },
  { label: "Analisi", path: ROUTES.analytics, icon: "insights", description: "Andamenti nel tempo" },
  { label: "Impostazioni", path: ROUTES.settings, icon: "tune", description: "Categorie, conti e aspetto" },
];

export const isNavItemActive = (item: NavItem, pathname: string) =>
  item.path === "/" ? pathname === "/" : pathname.startsWith(item.path);
