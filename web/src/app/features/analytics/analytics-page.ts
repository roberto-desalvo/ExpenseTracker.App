import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatButtonToggleModule } from "@angular/material/button-toggle";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { TransactionApi } from "../../core/api/transaction.api";
import { WIDTH_SPAN } from "../../core/config/analytics-cards";
import { ROUTES } from "../../core/config/routes";
import { drillDownParams } from "../../core/navigation/drill-down";
import { DrillDownFilter, MovementType } from "../../core/state/transactions-table.store";
import { TimeSeriesList } from "../../core/models/TimeSeries";
import { AccountsStore } from "../../core/state/accounts.store";
import { AnalyticsLayoutStore } from "../../core/state/analytics-layout.store";
import { CategoriesStore } from "../../core/state/categories.store";
import { TransactionsStore } from "../../core/state/transactions.store";
import { ThemeService } from "../../core/theme/theme.service";
import { addMonths, toDateInputValue, toIsoDateEnd, toIsoDateStart } from "../../core/utils/date.utilities";
import {
  enumerateMonthKeys,
  formatCurrency,
  formatPercent,
  formatPeriodLabel,
  formatSignedCurrency,
} from "../../core/utils/format.utilities";
import { CashFlowChart, CashFlowPoint } from "../../shared/charts/cash-flow-chart";
import { PeriodClick } from "../../shared/charts/chart-options";
import { ChartView, ChartViewToggle } from "../../shared/charts/chart-parts";
import { buildEntityColorMap } from "../../shared/charts/entityColors";
import { LineSeries, NetWorthAreaChart } from "../../shared/charts/net-worth-area-chart";
import { RankedBarList } from "../../shared/charts/ranked-bar-list";
import { StackSeries, StackedPeriodBars } from "../../shared/charts/stacked-period-bars";
import { EmptyState } from "../../shared/ui/empty-state";
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
      return { start: toDateInputValue(addMonths(today, -3)), end };
    case "6m":
      return { start: toDateInputValue(addMonths(today, -6)), end };
    case "24m":
      return { start: toDateInputValue(addMonths(today, -24)), end };
    case "ytd":
      return { start: toDateInputValue(new Date(today.getFullYear(), 0, 1)), end };
    default:
      return { start: toDateInputValue(addMonths(today, -12)), end };
  }
};

// stato della pagina nei query param (valori di default omessi): il back del browser ripristina la vista
type ParamPairs<T> = readonly (readonly [T, string])[];

const GRANULARITY_PARAMS: ParamPairs<Granularity> = [
  [1, "giorno"],
  [2, "settimana"],
  [3, "mese"],
  [4, "anno"],
];
const RANK_METRIC_PARAMS: ParamPairs<RankMetric> = [
  ["earned", "entrate"],
  ["spent", "uscite"],
  ["saved", "risparmio"],
  ["lost", "perdite"],
];
const RANK_GRANULARITY_PARAMS: ParamPairs<RankGranularity> = [
  [3, "mese"],
  [4, "anno"],
];
const CATEGORY_KIND_PARAMS: ParamPairs<"spent" | "earned"> = [
  ["spent", "uscite"],
  ["earned", "entrate"],
];
const VIEW_PARAMS: ParamPairs<ChartView> = [
  ["chart", "grafico"],
  ["table", "tabella"],
];
const PRESET_PARAMS: ParamPairs<RangePreset> = PRESETS.map((p) => [p.value, p.value] as const);

const decode = <T>(pairs: ParamPairs<T>, param: string | null, fallback: T): T =>
  pairs.find(([, p]) => p === param)?.[0] ?? fallback;

const encode = <T>(pairs: ParamPairs<T>, value: T, fallback: T): string | null =>
  value === fallback ? null : (pairs.find(([v]) => v === value)?.[1] ?? null);

const isDateParam = (value: string | null): value is string => !!value && /^\d{4}-\d{2}-\d{2}$/.test(value);

const dimensionId =(dimensions: { key: string; value: string }[], key: string) => {
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

/** Analisi: card scelte dall'utente (Impostazioni → Analisi) su flussi, patrimonio, periodi e categorie del periodo scelto. */
@Component({
  selector: "app-analytics-page",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainer,
    PageHeader,
    SurfaceCard,
    EmptyState,
    RouterLink,
    SummaryFigure,
    CashFlowChart,
    NetWorthAreaChart,
    StackedPeriodBars,
    RankedBarList,
    ChartViewToggle,
    MatButtonModule,
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
  private readonly layout = inject(AnalyticsLayoutStore);

  protected readonly cards = this.layout.layout;
  protected readonly span = WIDTH_SPAN;
  protected readonly settingsLink = ROUTES.settings;
  protected readonly settingsQuery = { sezione: "analisi" };

  protected readonly presets = PRESETS;
  protected readonly granularities = GRANULARITIES;
  protected readonly rankMetrics = RANK_METRICS;
  protected readonly rankSize = RANK_SIZE;
  protected readonly rankEmpty = RANK_EMPTY;
  protected readonly Infinity = Infinity;
  protected readonly formatCurrency = formatCurrency;
  protected readonly formatSignedCurrency = formatSignedCurrency;
  protected readonly formatPercent = formatPercent;

  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly initial = this.route.snapshot.queryParamMap;

  protected readonly preset = signal<RangePreset>(decode(PRESET_PARAMS, this.initial.get("periodo"), "12m"));
  protected readonly range = signal(this.initialRange());
  protected readonly granularity = signal<Granularity>(decode(GRANULARITY_PARAMS, this.initial.get("raggruppa"), 3));
  protected readonly loading = signal(false);

  private readonly total = signal<TimeSeriesList | null>(null);
  private readonly byAccount = signal<TimeSeriesList | null>(null);
  private readonly flow = signal<TimeSeriesList | null>(null);
  private readonly byCategory = signal<TimeSeriesList | null>(null);

  protected readonly flowView = signal<ChartView>(decode(VIEW_PARAMS, this.initial.get("flusso"), "chart"));
  protected readonly netWorthView = signal<ChartView>(decode(VIEW_PARAMS, this.initial.get("patrimonio"), "chart"));
  protected readonly rankMetric = signal<RankMetric>(decode(RANK_METRIC_PARAMS, this.initial.get("classifica"), "earned"));
  protected readonly rankGranularity = signal<RankGranularity>(
    decode(RANK_GRANULARITY_PARAMS, this.initial.get("classifica-per"), 3),
  );
  private readonly rankFlow = signal<TimeSeriesList | null>(null);
  protected readonly rankLoading = signal(false);
  protected readonly categoryView = signal<ChartView>(decode(VIEW_PARAMS, this.initial.get("categorie-vista"), "chart"));
  protected readonly categoryKind = signal<"spent" | "earned">(
    decode(CATEGORY_KIND_PARAMS, this.initial.get("categorie"), "spent"),
  );
  protected readonly rankCategoryKind = signal<"spent" | "earned">(
    decode(CATEGORY_KIND_PARAMS, this.initial.get("classifica-categorie"), "spent"),
  );

  // le chiamate servono solo alle card visibili
  private readonly needsAccounts = computed(() => this.layout.isVisible("net-worth"));
  private readonly needsCategories = computed(
    () => this.layout.isVisible("category-periods") || this.layout.isVisible("category-rank"),
  );
  private readonly needsRank = computed(() => this.layout.isVisible("period-rank"));

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
      return values.map((v) => ({ period: v.period, earned: v.earned, spent: v.spent, invested: v.invested }));
    }
    const byPeriod = new Map(values.map((v) => [v.period, v]));
    const { start, end } = this.range();
    return enumerateMonthKeys(new Date(`${start}T00:00:00`), new Date(`${end}T00:00:00`)).map((period) => ({
      period,
      earned: byPeriod.get(period)?.earned ?? 0,
      spent: byPeriod.get(period)?.spent ?? 0,
      invested: byPeriod.get(period)?.invested ?? 0,
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

  private categorySeriesOf(kind: "spent" | "earned"): StackSeries[] {
    const categories = this.categories.all();
    return (this.byCategory()?.series ?? []).map((s, index) => {
      const id = dimensionId(s.dimensions, "CategoryId");
      return {
        key: `cat-${Number.isFinite(id) ? id : index}`,
        name: categories.find((x) => x.id === id)?.name ?? `Categoria ${id}`,
        values: s.values.map((v) => ({ period: v.period, value: kind === "spent" ? v.spent : v.earned })),
      };
    });
  }

  protected readonly categorySeries = computed(() => this.categorySeriesOf(this.categoryKind()));

  // la classifica categorie ha un tipo (uscite/entrate) proprio: resta valida anche se l'altra card è nascosta
  protected readonly categoryTotals = computed(() =>
    this.categorySeriesOf(this.rankCategoryKind()).map((s) => ({
      id: s.key,
      name: s.name,
      value: s.values.reduce((sum, v) => sum + v.value, 0),
    })),
  );

  protected readonly summary = computed(() => {
    const cashFlow = this.cashFlow();
    const total = this.total()?.series[0];
    const earned = cashFlow.reduce((s, p) => s + p.earned, 0);
    const spent = cashFlow.reduce((s, p) => s + p.spent, 0);
    const invested = cashFlow.reduce((s, p) => s + p.invested, 0);
    // saldi puntuali (non le medie del primo/ultimo periodo del grafico): la variazione è la somma dei movimenti
    const startValue = total?.openingBalance ?? 0;
    const endValue = total?.closingBalance ?? 0;
    return {
      earned,
      spent,
      net: earned - spent,
      rate: earned > 0 ? ((earned - spent) / earned) * 100 : null,
      invested,
      investedRate: earned > 0 ? (invested / earned) * 100 : null,
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
    this.layout.ensureLoaded();

    effect(() => {
      const { start, end } = this.range();
      const granularity = this.granularity();
      const accountIds = this.needsAccounts() ? toIds(this.accountIdsKey()) : [];
      const categoryIds = this.needsCategories() ? toIds(this.categoryIdsKey()) : [];
      this.transactions.dataVersion();
      if (!this.lookupsReady() || this.invalidRange() || !start || !end) return;
      untracked(() => void this.loadSeries(start, end, granularity, accountIds, categoryIds));
    });

    // la classifica ha un raggruppamento proprio (mese/anno), indipendente da quello della pagina
    effect(() => {
      const { start, end } = this.range();
      const granularity = this.rankGranularity();
      this.transactions.dataVersion();
      if (!this.needsRank() || this.invalidRange() || !start || !end) return;
      untracked(() => void this.loadRank(start, end, granularity));
    });

    // i cambi di filtro sostituiscono la voce di cronologia corrente; solo il drill-down ne aggiunge una
    effect(() => {
      const preset = this.preset();
      const { start, end } = this.range();
      const queryParams = {
        periodo: encode(PRESET_PARAMS, preset, "12m"),
        dal: preset === "custom" ? start || null : null,
        al: preset === "custom" ? end || null : null,
        raggruppa: encode(GRANULARITY_PARAMS, this.granularity(), 3),
        flusso: encode(VIEW_PARAMS, this.flowView(), "chart"),
        patrimonio: encode(VIEW_PARAMS, this.netWorthView(), "chart"),
        classifica: encode(RANK_METRIC_PARAMS, this.rankMetric(), "earned"),
        "classifica-per": encode(RANK_GRANULARITY_PARAMS, this.rankGranularity(), 3),
        categorie: encode(CATEGORY_KIND_PARAMS, this.categoryKind(), "spent"),
        "categorie-vista": encode(VIEW_PARAMS, this.categoryView(), "chart"),
        "classifica-categorie": encode(CATEGORY_KIND_PARAMS, this.rankCategoryKind(), "spent"),
      };
      untracked(() => void this.router.navigate([], { relativeTo: this.route, queryParams, replaceUrl: true }));
    });
  }

  private initialRange() {
    const start = this.initial.get("dal");
    const end = this.initial.get("al");
    return this.preset() === "custom" && isDateParam(start) && isDateParam(end) ? { start, end } : rangeFor(this.preset());
  }

  /** Flusso di cassa → Movimenti del periodo: barra entrate = entrate, uscite/investimenti = uscite, altrove tutti. */
  protected openFlowPeriod(click: PeriodClick) {
    const movementType: MovementType =
      click.seriesKey === "earned" ? "incomes" : click.seriesKey === "spent" || click.seriesKey === "invested" ? "outcomes" : "all";
    this.openTransactions({ period: click.period, categoryIds: [], movementType });
  }

  /** Categorie → Movimenti del periodo, della categoria cliccata e del tipo mostrato (uscite/entrate). */
  protected openCategoryPeriod(click: PeriodClick) {
    const id = click.seriesKey?.startsWith("cat-") ? Number(click.seriesKey.slice(4)) : NaN;
    this.openTransactions({
      period: click.period,
      categoryIds: Number.isInteger(id) ? [id] : [],
      movementType: this.categoryKind() === "spent" ? "outcomes" : "incomes",
    });
  }

  private openTransactions(filter: DrillDownFilter) {
    void this.router.navigate([ROUTES.transactions], { queryParams: drillDownParams(filter, "analisi") });
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
