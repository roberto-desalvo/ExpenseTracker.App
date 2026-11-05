import { ChangeDetectionStrategy, Component, computed, inject, input } from "@angular/core";
import { MatTooltip } from "@angular/material/tooltip";
import { ThemeService } from "../../core/theme/theme.service";
import { formatCurrency, formatPercent } from "../../core/utils/format.utilities";
import { ChartEmpty } from "./chart-parts";

export interface RankedItem {
  id: number | string;
  name: string;
  value: number;
}

/**
 * Classifica a barre orizzontali: confronta importi tra voci senza ordine naturale
 * (categorie, conti). Un solo colore: la lunghezza porta il valore, il testo il nome.
 */
@Component({
  selector: "app-ranked-bar-list",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ChartEmpty, MatTooltip],
  host: { "[style.min-height.px]": "minHeight()" },
  template: `
    @if (model().rows.length === 0) {
      <app-chart-empty [height]="minHeight()" [message]="emptyMessage()" />
    } @else {
      @for (row of model().rows; track row.id) {
        @let share = model().total > 0 ? (row.value / model().total) * 100 : 0;
        @let isOther = row.id === "__other__";
        <div
          class="item"
          matTooltipPosition="above"
          [matTooltip]="row.name + ': ' + formatCurrency(row.value) + (showShare() ? ' · ' + formatPercent(share) + ' del totale' : '')"
        >
          <div class="head">
            <span class="name nowrap" [class.other]="isOther">{{ row.name }}</span>
            <span class="value">{{ formatCurrency(row.value) }}</span>
            @if (showShare()) {
              <span class="share tabular">{{ formatPercent(share, 0) }}</span>
            }
          </div>
          <div class="track">
            <div
              class="bar"
              [style.width.%]="barWidth(row.value)"
              [style.background-color]="isOther ? otherColor() : color()"
            ></div>
          </div>
        </div>
      }
    }
  `,
  styles: `
    :host { display: flex; flex-direction: column; gap: 12px; }
    .item { cursor: default; }
    .head { display: flex; align-items: baseline; gap: 8px; margin-bottom: 4px; }
    .name { flex: 1; min-width: 0; font-size: var(--app-text-md); font-weight: 500; color: var(--app-text); }
    .name.other { color: var(--app-text-secondary); }
    .value { font-size: var(--app-text-md); font-weight: 650; color: var(--app-text); }
    .share { font-size: var(--app-text-sm); color: var(--app-text-muted); width: 44px; text-align: right; }
    .track { height: 8px; border-radius: 4px; background-color: var(--app-surface-alt); overflow: hidden; }
    .bar { height: 100%; border-radius: 4px; transition: width .4s ease; }
  `,
})
export class RankedBarList {
  readonly items = input.required<RankedItem[]>();
  readonly color = input.required<string>();
  /** voci mostrate prima di raggruppare il resto in "Altre" */
  readonly maxItems = input(6);
  readonly otherLabel = input("Altre");
  readonly emptyMessage = input("Nessun movimento nel periodo");
  readonly minHeight = input(260);
  /** mostra la quota di ogni voce sul totale (non ha senso per le classifiche di periodi) */
  readonly showShare = input(true);

  private readonly theme = inject(ThemeService);
  protected readonly otherColor = computed(() => this.theme.tokens().chart.other);
  protected readonly formatCurrency = formatCurrency;
  protected readonly formatPercent = formatPercent;

  protected readonly model = computed(() => {
    const maxItems = this.maxItems();
    const positive = this.items()
      .map((item) => ({ ...item, value: Math.abs(item.value) }))
      .filter((item) => item.value > 0)
      .sort((x, y) => y.value - x.value);
    const total = positive.reduce((acc, item) => acc + item.value, 0);

    if (positive.length <= maxItems) {
      return { rows: positive, total };
    }

    const head = positive.slice(0, maxItems - 1);
    const tail = positive.slice(maxItems - 1);
    return {
      rows: [
        ...head,
        {
          id: "__other__" as number | string,
          name: `${this.otherLabel()} (${tail.length})`,
          value: tail.reduce((acc, item) => acc + item.value, 0),
        },
      ],
      total,
    };
  });

  protected barWidth(value: number) {
    const max = this.model().rows[0]?.value ?? 1;
    return Math.max((value / max) * 100, 1.5);
  }
}
