import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from "@angular/core";
import { MatButton } from "@angular/material/button";
import { MatButtonToggleModule } from "@angular/material/button-toggle";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltip } from "@angular/material/tooltip";
import { Router } from "@angular/router";
import { TransactionApi } from "../../core/api/transaction.api";
import { AuthService } from "../../core/auth/auth.service";
import { ROUTES } from "../../core/config/routes";
import { LandingDashboard } from "../../core/models/LandingDashboard";
import Transaction from "../../core/models/Transaction";
import { QuickActionsService } from "../../core/state/quick-actions.service";
import { TransactionsStore } from "../../core/state/transactions.store";
import { startOfMonth } from "../../core/utils/date.utilities";
import {
  enumerateMonthKeys,
  formatCurrency,
  formatMonthYear,
  formatPercent,
  formatSignedCurrency,
  toMonthKey,
} from "../../core/utils/format.utilities";
import { CashFlowPoint } from "../../shared/charts/cash-flow-chart";
import { CategoryDonutChart } from "../../shared/charts/category-donut-chart";
import { RankedBarList } from "../../shared/charts/ranked-bar-list";
import { PageContainer } from "../../shared/ui/page-container";
import { PageHeader } from "../../shared/ui/page-header";
import { SurfaceCard } from "../../shared/ui/surface-card";
import { AccountsOverview } from "./accounts-overview";
import { HeroBalanceCard } from "./hero-balance-card";
import { RecentTransactions } from "./recent-transactions";
import { StatTile } from "./stat-tile";

const BALANCE_VISIBILITY_STORAGE_KEY = "expense-tracker:home-balance-visible";

type BreakdownView = "bars" | "pie";
type BreakdownKind = "spent" | "earned";

const readBalanceVisible = () => {
  try {
    return window.localStorage.getItem(BALANCE_VISIBILITY_STORAGE_KEY) === "true";
  } catch {
    return false;
  }
};

const greeting = (date: Date) => {
  const hour = date.getHours();
  if (hour < 13) return "Buongiorno";
  if (hour < 18) return "Buon pomeriggio";
  return "Buonasera";
};

const average = (values: number[]) => (values.length ? values.reduce((s, v) => s + v, 0) / values.length : 0);

/** Variazione percentuale rispetto alla media, come frase breve. */
@Component({
  selector: "app-versus-average",
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (avg() <= 0) {
      <span>Nessuno storico per il confronto</span>
    } @else {
      <span>
        <strong [class.text-income]="good()" [class.text-expense]="!good()">
          {{ diff() >= 0 ? "+" : "−" }}{{ formatPercent(absDiff(), 0) }}
        </strong>
        rispetto alla media degli ultimi 6 mesi ({{ formatCurrency(avg()) }})
      </span>
    }
  `,
  styles: `strong { font-weight: 700; }`,
})
export class VersusAverage {
  readonly current = input.required<number>();
  readonly avg = input.required<number>();
  readonly lowerIsBetter = input(false);
  protected readonly formatPercent = formatPercent;
  protected readonly formatCurrency = formatCurrency;
  protected readonly diff = computed(() => ((this.current() - this.avg()) / this.avg()) * 100);
  protected readonly absDiff = computed(() => Math.abs(this.diff()));
  protected readonly good = computed(() => (this.lowerIsBetter() ? this.diff() <= 0 : this.diff() >= 0));
}

/** Dashboard: patrimonio, KPI del mese, conti, ripartizione per categoria e ultimi movimenti. */
@Component({
  selector: "app-dashboard-page",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainer,
    PageHeader,
    SurfaceCard,
    HeroBalanceCard,
    StatTile,
    AccountsOverview,
    RecentTransactions,
    RankedBarList,
    CategoryDonutChart,
    VersusAverage,
    MatButton,
    MatButtonToggleModule,
    MatIconModule,
    MatTooltip,
  ],
  templateUrl: "./dashboard-page.html",
  styleUrl: "./dashboard-page.scss",
})
export class DashboardPage {
  private readonly api = inject(TransactionApi);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly transactions = inject(TransactionsStore);
  protected readonly quickActions = inject(QuickActionsService);

  protected readonly loading = signal(true);
  protected readonly data = signal<LandingDashboard | null>(null);
  // flusso mensile degli ultimi 12 mesi: serve solo per le medie di confronto dei KPI
  private readonly cashFlow = signal<CashFlowPoint[]>([]);
  protected readonly recent = signal<Transaction[]>([]);
  protected readonly recentLoading = signal(true);
  protected readonly balanceVisible = signal(readBalanceVisible());
  protected readonly breakdownKind = signal<BreakdownKind>("spent");
  protected readonly breakdownView = signal<BreakdownView>("bars");

  protected readonly now = new Date();
  protected readonly Infinity = Infinity;
  protected readonly formatCurrency = formatCurrency;
  protected readonly formatSignedCurrency = formatSignedCurrency;
  protected readonly formatPercent = formatPercent;

  protected readonly eyebrow = this.now.toLocaleDateString("it-IT", { weekday: "long", day: "numeric", month: "long" });
  protected readonly title = computed(() => {
    const firstName = this.auth.userName().split(/[\s@]/)[0];
    return `${greeting(this.now)}${firstName ? `, ${firstName}` : ""}`;
  });
  protected readonly subtitle = `Ecco come stanno andando le tue finanze a ${formatMonthYear(this.now)}.`;

  protected readonly totals = computed(() => this.data()?.totals);
  protected readonly netWorthPoints = computed(() =>
    (this.data()?.netWorthSeries?.series?.[0]?.values ?? []).map((v) => ({ period: v.period, amount: v.amount })),
  );
  protected readonly asOfText = computed(() => {
    const asOf = this.data()?.asOf;
    return asOf ? new Date(asOf).toLocaleString("it-IT", { dateStyle: "short", timeStyle: "short" }) : "";
  });

  protected readonly history = computed(() => {
    const currentKey = toMonthKey(this.now);
    const past = this.cashFlow()
      .filter((p) => p.period !== currentKey)
      .slice(-6);
    return { avgSpent: average(past.map((p) => p.spent)), avgEarned: average(past.map((p) => p.earned)) };
  });

  protected readonly breakdownItems = computed(() =>
    (this.data()?.categories ?? []).map((c) => ({
      id: c.categoryId,
      name: c.name,
      value: this.breakdownKind() === "spent" ? c.spentMonth : c.earnedMonth,
    })),
  );
  protected readonly breakdownEmpty = computed(() =>
    this.breakdownKind() === "spent" ? "Nessuna uscita questo mese" : "Nessuna entrata questo mese",
  );

  protected readonly daysInMonth = new Date(this.now.getFullYear(), this.now.getMonth() + 1, 0).getDate();
  protected readonly monthProgress = (this.now.getDate() / this.daysInMonth) * 100;
  protected readonly savingsRate = computed(() => {
    const totals = this.totals();
    return totals && totals.earnedMonth > 0 ? (totals.netMonth / totals.earnedMonth) * 100 : null;
  });

  constructor() {
    // ricarica a ogni modifica dei movimenti (dataVersion)
    effect(() => {
      this.transactions.dataVersion();
      untracked(() => void this.load());
    });
  }

  private load() {
    const today = new Date();
    const firstMonth = startOfMonth(today, -11);

    const loadLanding = async () => {
      this.loading.set(true);
      try {
        this.data.set(await this.api.getLanding({ excludeTransfers: true }));
      } catch {
        this.data.set(null);
      } finally {
        this.loading.set(false);
      }
    };

    const loadCashFlow = async () => {
      try {
        const result = await this.api.getTimeSeries({
          startDate: firstMonth.toISOString(),
          endDate: today.toISOString(),
          idAccounts: [],
          idCategories: [],
          granularity: 3,
          excludeTransfers: true,
        });
        const byPeriod = new Map((result.series[0]?.values ?? []).map((p) => [p.period, p]));
        // il BE omette i mesi senza movimenti: li riempiamo a zero per non falsare l'asse
        this.cashFlow.set(
          enumerateMonthKeys(firstMonth, today).map((period) => ({
            period,
            earned: byPeriod.get(period)?.earned ?? 0,
            spent: byPeriod.get(period)?.spent ?? 0,
          })),
        );
      } catch {
        this.cashFlow.set([]);
      }
    };

    const loadRecent = async () => {
      this.recentLoading.set(true);
      try {
        const from = new Date(today);
        from.setDate(from.getDate() - 90);
        const result = await this.api.getAll({
          fromDate: from.toISOString(),
          toDate: today.toISOString(),
          page: 0,
          pageSize: 7,
        });
        this.recent.set(result.items);
      } catch {
        this.recent.set([]);
      } finally {
        this.recentLoading.set(false);
      }
    };

    return Promise.all([loadLanding(), loadCashFlow(), loadRecent()]);
  }

  protected toggleBalance() {
    this.balanceVisible.update((prev) => {
      try {
        window.localStorage.setItem(BALANCE_VISIBILITY_STORAGE_KEY, String(!prev));
      } catch {
        // ignora
      }
      return !prev;
    });
  }

  protected goTo(path: "analytics" | "transactions") {
    void this.router.navigateByUrl(ROUTES[path]);
  }

  protected setBreakdownView(value: BreakdownView | null) {
    if (value) this.breakdownView.set(value);
  }

  protected setBreakdownKind(value: BreakdownKind | null) {
    if (value) this.breakdownKind.set(value);
  }
}
