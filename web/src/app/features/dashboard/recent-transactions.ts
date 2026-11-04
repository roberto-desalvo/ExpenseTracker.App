import { ChangeDetectionStrategy, Component, input, output } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";
import Transaction from "../../core/models/Transaction";
import { formatSignedCurrency } from "../../core/utils/format.utilities";
import { EmptyState } from "../../shared/ui/empty-state";

const dayFormatter = new Intl.DateTimeFormat("it-IT", { day: "numeric", month: "short" });

/** Elenco compatto degli ultimi movimenti (feed stile app bancaria). */
@Component({
  selector: "app-recent-transactions",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule, EmptyState],
  template: `
    @if (loading()) {
      <div class="col gap-md">
        @for (i of [0, 1, 2, 3, 4, 5]; track i) {
          <div class="row gap-md">
            <span class="skeleton" style="width: 36px; height: 36px; border-radius: 6px"></span>
            <div class="spacer">
              <span class="skeleton skeleton-text" style="width: 60%"></span>
              <span class="skeleton skeleton-text" style="width: 35%"></span>
            </div>
            <span class="skeleton skeleton-text" style="width: 70px"></span>
          </div>
        }
      </div>
    } @else if (items().length === 0) {
      <app-empty-state
        icon="receipt_long"
        title="Nessun movimento recente"
        text="I movimenti degli ultimi 90 giorni compariranno qui."
      />
    } @else {
      <div class="list">
        @for (t of items(); track t.id) {
          @let isIncome = t.amount > 0;
          <button type="button" class="item" (click)="selected.emit(t)">
            <span class="icon" [class.income]="isIncome">
              <mat-icon class="icon-sm">{{ isIncome ? "south_west" : "north_east" }}</mat-icon>
            </span>
            <span class="col spacer">
              <span class="description nowrap">{{ t.description || "Senza descrizione" }}</span>
              <span class="meta nowrap">{{ day(t.date) }} · {{ t.category || "Senza categoria" }} · {{ t.account }}</span>
            </span>
            <span class="amount tabular" [class.text-income]="isIncome">{{ formatSignedCurrency(t.amount) }}</span>
          </button>
        }
      </div>
    }
  `,
  styles: `
    .list { display: flex; flex-direction: column; gap: 2px; }
    .item {
      display: flex; align-items: center; gap: 12px; padding: 8px; margin-inline: -8px; border-radius: var(--app-radius-control);
      border: none; background: none; text-align: left; font: inherit; cursor: pointer; width: calc(100% + 16px);
    }
    .item:hover { background-color: var(--app-surface-alt); }
    .icon {
      width: 36px; height: 36px; flex-shrink: 0; border-radius: var(--app-radius-avatar); display: grid; place-items: center;
      background-color: var(--app-expense-soft); color: var(--app-expense);
    }
    .icon.income { background-color: var(--app-income-soft); color: var(--app-income); }
    .description { font-size: 0.86rem; font-weight: 600; color: var(--app-text); }
    .meta { font-size: 0.74rem; color: var(--app-text-muted); }
    .amount { font-size: 0.88rem; font-weight: 700; color: var(--app-text); white-space: nowrap; }
  `,
})
export class RecentTransactions {
  readonly items = input.required<Transaction[]>();
  readonly loading = input(false);
  readonly selected = output<Transaction>();
  protected readonly formatSignedCurrency = formatSignedCurrency;

  protected day(value: Date | string) {
    return dayFormatter.format(new Date(value));
  }
}
