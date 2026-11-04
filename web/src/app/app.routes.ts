import { Routes } from "@angular/router";

export const routes: Routes = [
  { path: "", loadComponent: () => import("./features/dashboard/dashboard-page").then((m) => m.DashboardPage) },
  {
    path: "movimenti",
    loadComponent: () => import("./features/transactions/transactions-page").then((m) => m.TransactionsPage),
  },
  { path: "analisi", loadComponent: () => import("./features/analytics/analytics-page").then((m) => m.AnalyticsPage) },
  {
    path: "impostazioni",
    loadComponent: () => import("./features/settings/settings-page").then((m) => m.SettingsPage),
  },
  { path: "**", redirectTo: "" },
];
