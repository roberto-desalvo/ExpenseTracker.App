import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltipModule } from "@angular/material/tooltip";
import { ActivatedRoute, Router } from "@angular/router";
import { BudgetApi } from "../../core/api/budget.api";
import { NotificationService } from "../../core/api/notification.service";
import { BudgetMonth } from "../../core/models/Budget";
import { TransactionsStore } from "../../core/state/transactions.store";
import { startOfMonth, toDateInputValue } from "../../core/utils/date.utilities";
import { formatCurrency, formatPercent, formatSignedCurrency } from "../../core/utils/format.utilities";
import { EmptyState } from "../../shared/ui/empty-state";
import { MonthPicker } from "../../shared/ui/month-picker";
import { PageContainer } from "../../shared/ui/page-container";
import { PageHeader } from "../../shared/ui/page-header";
import { SurfaceCard } from "../../shared/ui/surface-card";
import { amountText, BudgetProgress, budgetProgress, parseAmount, planningMonths } from "./budget-plan";

interface PlanRow {
  categoryId: number;
  name: string;
  text: string;
  planned: number | null;
  invalid: boolean;
  spent: number | null;
  progress: BudgetProgress | null;
}

const monthDate = (value: string) => new Date(`${value.slice(0, 10)}T00:00:00`);

/**
 * Pianificazione: previsione di spesa per categoria, un mese alla volta. Mese corrente e futuri sono modificabili;
 * per il mese corrente (speso finora) e i passati si confronta la previsione con la spesa effettiva.
 */
@Component({
  selector: "app-planning-page",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageContainer, PageHeader, SurfaceCard, EmptyState, MonthPicker, MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: "./planning-page.html",
  styleUrl: "./planning-page.scss",
})
export class PlanningPage {
  private readonly api = inject(BudgetApi);
  private readonly notifications = inject(NotificationService);
  private readonly transactions = inject(TransactionsStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly formatCurrency = formatCurrency;
  protected readonly formatSignedCurrency = formatSignedCurrency;
  protected readonly formatPercent = formatPercent;
  protected readonly Math = Math;

  protected readonly months = planningMonths(new Date());
  private readonly currentMonth = toDateInputValue(startOfMonth(new Date()));

  protected readonly month = signal(this.initialMonth());
  protected readonly data = signal<BudgetMonth | null>(null);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  /** testo dei campi importo per categoria (solo mesi modificabili) */
  private readonly draft = signal<Record<number, string>>({});

  protected readonly selectedOption = computed(() => this.months.find((m) => m.startDate === this.month()) ?? null);
  protected readonly monthIndex = computed(() => this.months.findIndex((m) => m.startDate === this.month()));
  protected readonly editable = computed(() => this.data()?.isEditable ?? false);
  protected readonly hasActuals = computed(() => this.data()?.hasActuals ?? false);
  protected readonly isCurrent = computed(() => this.month() === this.currentMonth);

  protected readonly rows = computed<PlanRow[]>(() => {
    const data = this.data();
    if (!data) return [];
    const draft = this.draft();
    return data.lines.map((line) => {
      const text = data.isEditable ? (draft[line.categoryId] ?? "") : amountText(line.planned);
      const parsed = data.isEditable ? parseAmount(text) : line.planned;
      const invalid = parsed !== null && Number.isNaN(parsed);
      const planned = invalid || parsed === 0 ? null : parsed;
      return {
        categoryId: line.categoryId,
        name: line.name,
        text,
        planned,
        invalid,
        spent: line.spent,
        progress: budgetProgress(planned, line.spent),
      };
    });
  });

  /** nei mesi passati si mostrano solo le categorie con previsione o con spesa */
  protected readonly visibleRows = computed(() =>
    this.editable() ? this.rows() : this.rows().filter((r) => r.planned !== null || (r.spent ?? 0) > 0),
  );

  protected readonly dirty = computed(() => {
    const data = this.data();
    if (!data?.isEditable) return false;
    const saved = new Map(data.lines.map((l) => [l.categoryId, l.planned]));
    return this.rows().some((r) => r.invalid || (r.planned ?? null) !== (saved.get(r.categoryId) ?? null));
  });
  protected readonly invalid = computed(() => this.rows().some((r) => r.invalid));

  protected readonly summary = computed(() => {
    const data = this.data();
    const rows = this.rows();
    const planned = rows.reduce((sum, r) => sum + (r.planned ?? 0), 0);
    if (!data?.hasActuals || data.totalSpent === null) return { planned, spent: null, unplanned: null, progress: null };
    // la spesa senza categoria è nel totale ma in nessuna riga
    const uncategorized = data.totalSpent - data.lines.reduce((sum, l) => sum + (l.spent ?? 0), 0);
    const unplanned = rows.filter((r) => r.planned === null).reduce((sum, r) => sum + (r.spent ?? 0), 0) + uncategorized;
    return { planned, spent: data.totalSpent, unplanned, progress: budgetProgress(planned, data.totalSpent) };
  });

  constructor() {
    effect(() => {
      const month = this.month();
      this.transactions.dataVersion();
      untracked(() => void this.load(month));
    });
  }

  private initialMonth(): string {
    const param = this.route.snapshot.queryParamMap.get("mese");
    const fromParam = param && /^\d{4}-\d{2}$/.test(param) ? `${param}-01` : null;
    return fromParam && this.months.some((m) => m.startDate === fromParam) ? fromParam : this.currentMonth;
  }

  private async load(month: string) {
    this.loading.set(true);
    try {
      const data = await this.api.getMonth(month);
      if (this.month() !== month) return;
      this.data.set(data);
      this.draft.set(Object.fromEntries(data.lines.map((l) => [l.categoryId, amountText(l.planned)])));
    } catch {
      // errore già notificato dal livello API
    } finally {
      if (this.month() === month) this.loading.set(false);
    }
  }

  protected goTo(index: number) {
    const option = this.months[index];
    if (!option || this.dirty()) return;
    this.month.set(option.startDate);
    this.data.set(null);
    void this.router.navigate([], {
      queryParams: { mese: option.startDate === this.currentMonth ? null : option.startDate.slice(0, 7) },
      queryParamsHandling: "merge",
      replaceUrl: true,
    });
  }

  protected selectMonth(startDate: string) {
    this.goTo(this.months.findIndex((m) => m.startDate === startDate));
  }

  protected setAmount(categoryId: number, event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.draft.update((d) => ({ ...d, [categoryId]: value }));
  }

  protected discard() {
    const data = this.data();
    if (data) this.draft.set(Object.fromEntries(data.lines.map((l) => [l.categoryId, amountText(l.planned)])));
  }

  /** Riporta nei campi le previsioni del mese precedente (da salvare). */
  protected async copyPrevious() {
    const month = this.month();
    const previous = toDateInputValue(startOfMonth(monthDate(month), -1));
    try {
      const data = await this.api.getMonth(previous);
      if (this.month() !== month) return;
      if (!data.lines.some((l) => l.planned !== null)) {
        this.notifications.showError("Nessuna previsione nel mese precedente");
        return;
      }
      const planned = new Map(data.lines.map((l) => [l.categoryId, l.planned]));
      this.draft.update((d) => Object.fromEntries(Object.keys(d).map((id) => [id, amountText(planned.get(Number(id)))])));
    } catch {
      // errore già notificato dal livello API
    }
  }

  protected async save() {
    if (this.invalid() || this.saving()) return;
    const month = this.month();
    this.saving.set(true);
    try {
      await this.api.saveMonth({
        month,
        lines: this.rows()
          .filter((r) => r.planned !== null)
          .map((r) => ({ categoryId: r.categoryId, amount: r.planned })),
      });
      this.notifications.showSuccess("Pianificazione salvata");
      await this.load(month);
    } catch {
      // errore già notificato dal livello API
    } finally {
      this.saving.set(false);
    }
  }
}
