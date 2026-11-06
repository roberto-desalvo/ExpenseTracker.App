import { Routes } from "@angular/router";
import { adminGuard } from "./core/auth/admin.guard";

export const routes: Routes = [
  { path: "", loadComponent: () => import("./features/dashboard/dashboard-page").then((m) => m.DashboardPage) },
  {
    path: "movimenti",
    loadComponent: () => import("./features/transactions/transactions-page").then((m) => m.TransactionsPage),
  },
  {
    path: "movimenti/import",
    loadComponent: () => import("./features/imports/imports-page").then((m) => m.ImportsPage),
  },
  {
    path: "movimenti/abbinamento-excel",
    canMatch: [adminGuard],
    loadComponent: () => import("./features/legacy-match/legacy-match-page").then((m) => m.LegacyMatchPage),
  },
  { path: "analisi", loadComponent: () => import("./features/analytics/analytics-page").then((m) => m.AnalyticsPage) },
  {
    path: "impostazioni",
    loadComponent: () => import("./features/settings/settings-page").then((m) => m.SettingsPage),
  },
  { path: "**", redirectTo: "" },
];
