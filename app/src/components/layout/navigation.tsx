import { ReactNode } from "react";
import {
  InsightsRounded,
  ReceiptLongRounded,
  SpaceDashboardRounded,
  TuneRounded,
} from "@mui/icons-material";
import { ROUTES } from "../../config/routes";

export interface NavItem {
  label: string;
  path: string;
  icon: ReactNode;
  description: string;
}

export const NAV_ITEMS: NavItem[] = [
  {
    label: "Dashboard",
    path: ROUTES.dashboard,
    icon: <SpaceDashboardRounded fontSize="small" />,
    description: "Panoramica del mese e del patrimonio",
  },
  {
    label: "Movimenti",
    path: ROUTES.transactions,
    icon: <ReceiptLongRounded fontSize="small" />,
    description: "Elenco transazioni con filtri",
  },
  {
    label: "Analisi",
    path: ROUTES.analytics,
    icon: <InsightsRounded fontSize="small" />,
    description: "Andamenti nel tempo",
  },
  {
    label: "Impostazioni",
    path: ROUTES.settings,
    icon: <TuneRounded fontSize="small" />,
    description: "Categorie, conti e aspetto",
  },
];

export const isNavItemActive = (item: NavItem, pathname: string) =>
  item.path === "/" ? pathname === "/" : pathname.startsWith(item.path);
