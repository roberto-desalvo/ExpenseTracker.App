import { TimeSeriesList } from "./TimeSeries";

export interface LandingAccountBalance {
  accountId: number;
  name: string;
  currentBalance: number;
}

export interface LandingCategorySummary {
  categoryId: number;
  name: string;
  spentMonth: number;
  earnedMonth: number;
  netMonth: number;
}

export interface LandingTotals {
  currentBalanceTotal: number;
  spentMonth: number;
  earnedMonth: number;
  netMonth: number;
}

export interface LandingDashboard {
  asOf: string;
  monthStart: string;
  accounts: LandingAccountBalance[];
  categories: LandingCategorySummary[];
  totals: LandingTotals;
  netWorthSeries: TimeSeriesList;
}
