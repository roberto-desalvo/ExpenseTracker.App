import { ChangeDetectionStrategy, Component, computed, inject, input } from "@angular/core";
import { MatTooltip } from "@angular/material/tooltip";
import { LandingAccountBalance } from "../../core/models/LandingDashboard";
import { ThemeService } from "../../core/theme/theme.service";
import { formatCurrency, formatPercent, formatSignedCurrency } from "../../core/utils/format.utilities";
import { buildEntityColorMap } from "../../shared/charts/entityColors";
import { EmptyState } from "../../shared/ui/empty-state";

/** Ripartizione del patrimonio per conto: barra 100% + elenco con saldo e movimento del mese. */
@Component({
  selector: "app-accounts-overview",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatTooltip, EmptyState],
  template: `
    @if (visible().length === 0) {
      <app-empty-state title="Nessun conto" text="Aggiungi un conto dalle impostazioni per iniziare." />
    } @else {
      @if (positiveTotal() > 0) {
        <div class="share-bar" role="img" aria-label="Ripartizione del saldo positivo per conto">
          @for (x of positive(); track x.accountId) {
            @let share = (x.currentBalance / positiveTotal()) * 100;
            <span
              class="segment"
              [style.flex]="share + ' 1 0'"
              [style.background-color]="colorOf(x.accountId)"
              [matTooltip]="x.name + ' · ' + formatPercent(share)"
            ></span>
          }
        </div>
      }

      <div class="list">
        @for (x of sorted(); track x.accountId) {
          @let share = positiveTotal() > 0 && x.currentBalance > 0 ? (x.currentBalance / positiveTotal()) * 100 : null;
          <div class="item">
            <span class="dot" [style.background-color]="colorOf(x.accountId)"></span>
            <div class="col spacer">
              <span class="name nowrap">{{ x.name }}</span>
              <span class="share">{{ share !== null ? formatPercent(share) + " del totale" : "—" }}</span>
            </div>
            <div class="amounts">
              <span class="balance">{{ hidden() ? "••••••" : formatCurrency(x.currentBalance) }}</span>
              <span class="month" [class.text-income]="x.netMonth >= 0" [class.text-expense]="x.netMonth < 0">
                {{ formatSignedCurrency(x.netMonth) }} questo mese
              </span>
            </div>
          </div>
        }
      </div>
    }
  `,
  styles: `
    :host { display: flex; flex-direction: column; gap: 20px; }
    .share-bar { display: flex; gap: 2px; height: 12px; border-radius: 6px; overflow: hidden; }
    .segment { min-width: 3px; }
    .list { display: flex; flex-direction: column; gap: 2px; }
    .item {
      display: flex; align-items: center; gap: 12px; padding: 8px; margin-inline: -8px; border-radius: var(--app-radius-control);
    }
    .item:hover { background-color: var(--app-surface-alt); }
    .dot { width: 10px; height: 10px; border-radius: 3px; flex-shrink: 0; }
    .name { font-size: var(--app-text-md); font-weight: 600; color: var(--app-text); }
    .share { font-size: var(--app-text-sm); color: var(--app-text-muted); }
    .amounts { display: flex; flex-direction: column; text-align: right; }
    .balance { font-size: var(--app-text-md); font-weight: 650; color: var(--app-text); }
    .month { font-size: var(--app-text-sm); font-weight: 600; }
  `,
})
export class AccountsOverview {
  readonly accounts = input.required<LandingAccountBalance[]>();
  readonly hidden = input(false);

  private readonly theme = inject(ThemeService);
  protected readonly formatCurrency = formatCurrency;
  protected readonly formatSignedCurrency = formatSignedCurrency;
  protected readonly formatPercent = formatPercent;

  private readonly colorMap = computed(() => {
    const chart = this.theme.tokens().chart;
    return buildEntityColorMap(
      this.accounts().map((x) => x.accountId),
      chart.series,
      chart.other,
    );
  });

  /** Conti con saldo non nullo (arrotondato al centesimo): quelli a zero non vengono mostrati. */
  protected readonly visible = computed(() => this.accounts().filter((x) => Math.round(x.currentBalance * 100) !== 0));

  protected readonly sorted = computed(() => [...this.visible()].sort((x, y) => y.currentBalance - x.currentBalance));
  protected readonly positive = computed(() => this.sorted().filter((x) => x.currentBalance > 0));
  protected readonly positiveTotal = computed(() =>
    this.sorted().reduce((sum, x) => sum + Math.max(x.currentBalance, 0), 0),
  );

  protected colorOf(id: number) {
    return this.colorMap().get(id);
  }
}
