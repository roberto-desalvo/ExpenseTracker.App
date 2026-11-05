import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from "@angular/core";
import { MatButtonToggleModule } from "@angular/material/button-toggle";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { TransactionApi } from "../../core/api/transaction.api";
import { TimeSeriesList } from "../../core/models/TimeSeries";
import { AccountsStore } from "../../core/state/accounts.store";
import { CategoriesStore } from "../../core/state/categories.store";
import { TransactionsStore } from "../../core/state/transactions.store";
import { ThemeService } from "../../core/theme/theme.service";
import { startOfMonth, toDateInputValue, toIsoDateEnd, toIsoDateStart } from "../../core/utils/date.utilities";
import {
  enumerateMonthKeys,
  formatCurrency,
  formatPercent,
  formatPeriodLabel,
  formatSignedCurrency,
} from "../../core/utils/format.utilities";
import { CashFlowChart, CashFlowPoint } from "../../shared/charts/cash-flow-chart";
import { ChartView, ChartViewToggle } from "../../shared/charts/chart-parts";
import { buildEntityColorMap } from "../../shared/charts/entityColors";
import { LineSeries, NetWorthAreaChart } from "../../shared/charts/net-worth-area-chart";
import { RankedBarList } from "../../shared/charts/ranked-bar-list";
import { StackSeries, StackedPeriodBars } from "../../shared/charts/stacked-period-bars";
import { PageContainer } from "../../shared/ui/page-container";
import { PageHeader } from "../../shared/ui/page-header";
import { SurfaceCard } from "../../shared/ui/surface-card";

type RangePreset = "3m" | "6m" | "12m" | "24m" | "ytd" | "custom";
type Granularity = 1 | 2 | 3 | 4;
type RankMetric = "earned" | "spent" | "saved" | "lost";
type RankGranularity = 3 | 4;

const PRESETS: { value: RangePreset; label: string }[] = [
  { value: "3m", label: "3 mesi" },
  { value: "6m", label: "6 mesi" },
  { value: "12m", label: "12 mesi" },
  { value: "24m", label: "24 mesi" },
  { value: "ytd", label: "Da inizio anno" },
  { value: "custom", label: "Personalizzato" },
];

const GRANULARITIES: { value: Granularity; label: string }[] = [
  { value: 1, label: "Giorno" },
  { value: 2, label: "Settimana" },
  { value: 3, label: "Mese" },
  { value: 4, label: "Anno" },
];

const RANK_METRICS: { value: RankMetric; label: string }[] = [
  { value: "earned", label: "Entrate" },
  { value: "spent", label: "Uscite" },
  { value: "saved", label: "Risparmio" },
  { value: "lost", label: "Perdite" },
];

const RANK_SIZE = 8;

const RANK_EMPTY: Record<RankMetric, string> = {
  earned: "Nessuna entrata nel periodo",
  spent: "Nessuna uscita nel periodo",
  saved: "Nessun periodo in risparmio",
  lost: "Nessun periodo in perdita",
};

const rangeFor = (preset: RangePreset): { start: string; end: string } => {
  const today = new Date();
  const end = toDateInputValue(today);
  switch (preset) {
    case "3m":
      return { start: toDateInputValue(startOfMonth(today, -2)), end };
    case "6m":
      return { start: toDateInputValue(startOfMonth(today, -5)), end };
    case "24m":
      return { start: toDateInputValue(startOfMonth(today, -23)), end };
    case "ytd":
      return { start: toDateInputValue(new Date(today.getFullYear(), 0, 1)), end };
    default:
      return { start: toDateInputValue(startOfMonth(today, -11)), end };
  }
};

const dimensionId = (dimensions: { key: string; value: string }[], key: string) => {
  const raw = dimensions.find((d) => d.key === key)?.value;
  return raw === undefined ? NaN : Number(raw);
};

const toIds = (key: string) => (key ? key.split(",").map(Number) : []);

@Component({
  selector: "app-summary-figure",
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p class="label">{{ label() }}</p>
    <p class="value heading-font" [style.color]="tone() ?? null">{{ value() }}</p>
    @if (hint()) {
      <p class="hint">{{ hint() }}</p>
    }
  `,
  styles: `
    :host { display: block; min-width: 0; }
    .label { font-size: var(--app-text-sm); font-weight: 600; color: var(--app-text-secondary); }
    .value { font-size: var(--app-text-lg); font-weight: 750; letter-spacing: -0.02em; color: var(--app-text); line-height: 1.3; }
    .hint { font-size: var(--app-text-sm); color: var(--app-text-muted); }
  `,
})
export class SummaryFigure {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly hint = input<string>();
  readonly tone = input<string>();
}

/** Analisi: flusso di cassa, patrimonio, classifiche di periodi e categorie sul periodo scelto. */
@Component({
  selector: "app-analytics-page",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainer,
    PageHeader,
    SurfaceCard,
    SummaryFigure,
    CashFlowChart,
    NetWorthAreaChart,
    StackedPeriodBars,
    RankedBarList,
    ChartViewToggle,
    MatButtonToggleModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
  ],
  templateUrl: "./analytics-page.html",
  styleUrl: "./analytics-page.scss",
})
export class AnalyticsPage {
  private readonly api = inject(TransactionApi);
  private readonly accounts = inject(AccountsStore);
  private readonly categories = inject(CategoriesStore);
  private readonly transactions = inject(TransactionsStore);
  private readonly theme = inject(ThemeService);

  protected readonly presets = PRESETS;
  protected readonly granularities = GRANULARITIES;
  protected readonly rankMetrics = RANK_METRICS;
  protected readonly rankSize = RANK_SIZE;
  protected readonly rankEmpty = RANK_EMPTY;
  protected readonly Infinity = Infinity;
  protected readonly formatCurrency = formatCurrency;
  protected readonly formatSignedCurrency = formatSignedCurrency;
  protected readonly formatPercent = formatPercent;

  protected readonly preset = signal<RangePreset>("12m");
  protected readonly range = signal(rangeFor("12m"));
  protected readonly granularity = signal<Granularity>(3);
  protected readonly loading = signal(false);

  private readonly total = signal<TimeSeriesList | null>(null);
  private readonly byAccount = signal<TimeSeriesList | null>(null);
  private readonly flow = signal<TimeSeriesList | null>(null);
  private readonly byCategory = signal<TimeSeriesList | null>(null);

  protected readonly flowView = signal<ChartView>("chart");
  protected readonly netWorthView = signal<ChartView>("chart");
  protected readonly rankMetric = signal<RankMetric>("earned");
  protected readonly rankGranularity = signal<RankGranularity>(3);
  private readonly rankFlow = signal<TimeSeriesList | null>(null);
  protected readonly rankLoading = signal(false);
  protected readonly categoryView = signal<ChartView>("chart");
  protected readonly categoryKind = signal<"spent" | "earned">("spent");

  protected readonly invalidRange = computed(() => this.range().start > this.range().end);
  // chiavi stabili: un nuovo array con gli stessi id (es. dopo una rinomina) non deve far ripartire le chiamate
  private readonly accountIdsKey = computed(() =>
    this.accounts
      .all()
      .map((x) => x.id)
      .join(","),
  );
  private readonly categoryIdsKey = computed(() =>
    this.categories
      .all()
      .map((x) => x.id)
      .join(","),
  );
  // le serie per conto/categoria dipendono dalle anagrafiche: si attende il loro primo caricamento
  private readonly lookupsReady = computed(() => this.accounts.loaded() && this.categories.loaded());

  protected readonly tokens = this.theme.tokens;

  protected readonly rankItems = computed(() => {
    const metric = this.rankMetric();
    return (
      (this.rankFlow()?.series[0]?.values ?? [])
        .map((v) => {
          const net = v.earned - v.spent;
          const value = metric === "earned" ? v.earned : metric === "spent" ? v.spent : metric === "saved" ? net : -net;
          return { id: v.period, name: formatPeriodLabel(v.period, true), value };
        })
        // risparmio e perdite considerano solo i periodi con saldo netto del segno giusto
        .filter((item) => item.value > 0)
        .sort((x, y) => y.value - x.value)
        .slice(0, RANK_SIZE)
    );
  });

  protected readonly netWorth = computed(() =>
    (this.total()?.series[0]?.values ?? []).map((v) => ({ period: v.period, amount: v.amount })),
  );

  protected readonly cashFlow = computed<CashFlowPoint[]>(() => {
    const values = this.flow()?.series[0]?.values ?? [];
    if (this.granularity() !== 3) {
      return values.map((v) => ({ period: v.period, earned: v.earned, spent: v.spent }));
    }
    const byPeriod = new Map(values.map((v) => [v.period, v]));
    const { start, end } = this.range();
    return enumerateMonthKeys(new Date(`${start}T00:00:00`), new Date(`${end}T00:00:00`)).map((period) => ({
      period,
      earned: byPeriod.get(period)?.earned ?? 0,
      spent: byPeriod.get(period)?.spent ?? 0,
    }));
  });

  protected readonly accountSeries = computed<LineSeries[]>(() => {
    const chart = this.tokens().chart;
    const colors = buildEntityColorMap(toIds(this.accountIdsKey()), chart.series, chart.other);
    const accounts = this.accounts.all();
    return (this.byAccount()?.series ?? []).map((s, index) => {
      const id = dimensionId(s.dimensions, "AccountId");
      return {
        key: `acc-${Number.isFinite(id) ? id : index}`,
        name: accounts.find((x) => x.id === id)?.name ?? `Conto ${id}`,
        color: colors.get(id) ?? chart.other,
        values: s.values.map((v) => ({ period: v.period, amount: v.amount })),
      };
    });
  });

  protected readonly categorySeries = computed<StackSeries[]>(() => {
    const kind = this.categoryKind();
    const categories = this.categories.all();
    return (this.byCategory()?.series ?? []).map((s, index) => {
      const id = dimensionId(s.dimensions, "CategoryId");
      return {
        key: `cat-${Number.isFinite(id) ? id : index}`,
        name: categories.find((x) => x.id === id)?.name ?? `Categoria ${id}`,
        values: s.values.map((v) => ({ period: v.period, value: kind === "spent" ? v.spent : v.earned })),
      };
    });
  });

  protected readonly categoryTotals = computed(() =>
    this.categorySeries().map((s) => ({ id: s.key, name: s.name, value: s.values.reduce((sum, v) => sum + v.value, 0) })),
  );

  protected readonly summary = computed(() => {
    const cashFlow = this.cashFlow();
    const netWorth = this.netWorth();
    const earned = cashFlow.reduce((s, p) => s + p.earned, 0);
    const spent = cashFlow.reduce((s, p) => s + p.spent, 0);
    const startValue = netWorth[0]?.amount ?? 0;
    const endValue = netWorth[netWorth.length - 1]?.amount ?? 0;
    return {
      earned,
      spent,
      net: earned - spent,
      rate: earned > 0 ? ((earned - spent) / earned) * 100 : null,
      startValue,
      endValue,
      change: endValue - startValue,
    };
  });

  protected readonly periodLabel = computed(() => {
    const { start, end } = this.range();
    const fmt = (value: string) => new Date(`${value}T00:00:00`).toLocaleDateString("it-IT", { dateStyle: "medium" });
    return `${fmt(start)} – ${fmt(end)}`;
  });

  constructor() {
    effect(() => {
      const { start, end } = this.range();
      const granularity = this.granularity();
      const accountIds = toIds(this.accountIdsKey());
      const categoryIds = toIds(this.categoryIdsKey());
      this.transactions.dataVersion();
      if (!this.lookupsReady() || this.invalidRange() || !start || !end) return;
      untracked(() => void this.loadSeries(start, end, granularity, accountIds, categoryIds));
    });

    // la classifica ha un raggruppamento proprio (mese/anno), indipendente da quello della pagina
    effect(() => {
      const { start, end } = this.range();
      const granularity = this.rankGranularity();
      this.transactions.dataVersion();
      if (this.invalidRange() || !start || !end) return;
      untracked(() => void this.loadRank(start, end, granularity));
    });
  }

  private async loadSeries(start: string, end: string, granularity: Granularity, accountIds: number[], categoryIds: number[]) {
    const base = { startDate: toIsoDateStart(start), endDate: toIsoDateEnd(end), granularity };
    this.loading.set(true);
    try {
      const [t, acc, f, cat] = await Promise.all([
        // il saldo (stock) considera sempre i trasferimenti: sono movimenti reali per il singolo conto
        this.api.getStock({ ...base, idAccounts: [], idCategories: [] }),
        accountIds.length ? this.api.getStock({ ...base, idAccounts: accountIds, idCategories: [] }) : Promise.resolve(null),
        this.api.getTimeSeries({ ...base, idAccounts: [], idCategories: [], excludeTransfers: true }),
        categoryIds.length
          ? this.api.getTimeSeries({ ...base, idAccounts: [], idCategories: categoryIds, excludeTransfers: true })
          : Promise.resolve(null),
      ]);
      this.total.set(t);
      this.byAccount.set(acc);
      this.flow.set(f);
      this.byCategory.set(cat);
    } catch {
      // errore già mostrato dal layer API
    } finally {
      this.loading.set(false);
    }
  }

  private async loadRank(start: string, end: string, granularity: RankGranularity) {
    this.rankLoading.set(true);
    try {
      this.rankFlow.set(
        await this.api.getTimeSeries({
          startDate: toIsoDateStart(start),
          endDate: toIsoDateEnd(end),
          granularity,
          idAccounts: [],
          idCategories: [],
          excludeTransfers: true,
        }),
      );
    } catch {
      // errore già mostrato dal layer API
    } finally {
      this.rankLoading.set(false);
    }
  }

  protected choosePreset(next: RangePreset | null) {
    if (!next) return;
    this.preset.set(next);
    if (next !== "custom") this.range.set(rangeFor(next));
  }

  protected setRangeValue(key: "start" | "end", event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.range.update((r) => ({ ...r, [key]: value }));
  }

  protected pick<T>(target: { set: (value: T) => void }, value: T | null) {
    if (value !== null && value !== undefined) target.set(value);
  }
}
