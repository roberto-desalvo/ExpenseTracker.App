import { ChangeDetectionStrategy, Component, computed, inject, input } from "@angular/core";
import type { EChartsCoreOption } from "echarts/core";
import { NgxEchartsDirective } from "ngx-echarts";
import { ThemeService } from "../../core/theme/theme.service";
import { formatCurrency, formatPercent } from "../../core/utils/format.utilities";
import { ChartEmpty, tooltipBase, tooltipHtml } from "./chart-parts";
import { RankedItem } from "./ranked-bar-list";

interface Slice {
  id: number | string;
  name: string;
  value: number;
  share: number;
  color: string;
  opacity: number;
}

/**
 * Composizione di un totale (parti di un intero) come ciambella. Le fette sono colorate
 * nell'ordine categoriale fisso, dalla più grande; oltre la palette i colori ripartono
 * con opacità ridotta, così ogni fetta resta distinguibile dalle vicine.
 */
@Component({
  selector: "app-category-donut-chart",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgxEchartsDirective, ChartEmpty],
  host: { "[style.min-height.px]": "minHeight()" },
  template: `
    @if (model().slices.length === 0) {
      <app-chart-empty [height]="minHeight()" [message]="emptyMessage()" />
    } @else {
      <div class="donut">
        <div echarts class="chart" [options]="options()"></div>
        <!-- totale al centro della ciambella -->
        <div class="center">
          <div>
            <p class="total-label">Totale</p>
            <p class="total heading-font">{{ formatCurrency(model().total) }}</p>
          </div>
        </div>
      </div>
      <div class="list">
        @for (slice of model().slices; track slice.id) {
          <div class="row gap-sm">
            <span class="dot" [style.background-color]="slice.color" [style.opacity]="slice.opacity"></span>
            <span class="name nowrap" [class.other]="slice.id === '__other__'">{{ slice.name }}</span>
            <span class="value tabular">{{ formatCurrency(slice.value) }}</span>
            <span class="share tabular">{{ formatPercent(slice.share, 0) }}</span>
          </div>
        }
      </div>
    }
  `,
  styles: `
    :host { display: flex; flex-direction: column; gap: 16px; }
    .donut { position: relative; width: 100%; height: 190px; }
    .chart { width: 100%; height: 100%; }
    .center { position: absolute; inset: 0; display: grid; place-items: center; pointer-events: none; text-align: center; }
    .total-label { font-size: var(--app-text-sm); color: var(--app-text-muted); }
    .total { font-size: var(--app-text-md); font-weight: 750; color: var(--app-text); letter-spacing: -0.01em; }
    .list { display: flex; flex-direction: column; gap: 8px; }
    .dot { width: 9px; height: 9px; border-radius: 2px; flex-shrink: 0; }
    .name { flex: 1; min-width: 0; font-size: var(--app-text-md); color: var(--app-text); }
    .name.other { color: var(--app-text-secondary); }
    .value { font-size: var(--app-text-md); font-weight: 650; color: var(--app-text); }
    .share { font-size: var(--app-text-sm); color: var(--app-text-muted); width: 40px; text-align: right; }
  `,
})
export class CategoryDonutChart {
  readonly items = input.required<RankedItem[]>();
  /** fette mostrate prima di raggruppare il resto in "Altre" (inclusa); se omesso mostra tutte le voci */
  readonly maxSlices = input(Infinity);
  readonly otherLabel = input("Altre");
  readonly emptyMessage = input("Nessun movimento nel periodo");
  readonly minHeight = input(260);

  private readonly theme = inject(ThemeService);
  protected readonly formatCurrency = formatCurrency;
  protected readonly formatPercent = formatPercent;

  protected readonly model = computed(() => {
    const a = this.theme.tokens();
    const maxSlices = this.maxSlices();
    const positive = this.items()
      .map((item) => ({ ...item, value: Math.abs(item.value) }))
      .filter((item) => item.value > 0)
      .sort((x, y) => y.value - x.value);
    const sum = positive.reduce((acc, item) => acc + item.value, 0);
    const head = positive.length <= maxSlices ? positive : positive.slice(0, maxSlices - 1);
    const tail = positive.slice(head.length);

    const palette = a.chart.series;
    const slices: Slice[] = head.map((item, index) => {
      const round = palette.length ? Math.floor(index / palette.length) : 0;
      return {
        ...item,
        share: sum > 0 ? (item.value / sum) * 100 : 0,
        color: palette.length ? palette[index % palette.length] : a.chart.other,
        opacity: Math.max(0.3, 1 - round * 0.4),
      };
    });
    if (tail.length) {
      const value = tail.reduce((acc, item) => acc + item.value, 0);
      slices.push({
        id: "__other__",
        name: `${this.otherLabel()} (${tail.length})`,
        value,
        share: sum > 0 ? (value / sum) * 100 : 0,
        color: a.chart.other,
        opacity: 1,
      });
    }
    return { slices, total: sum };
  });

  protected readonly options = computed<EChartsCoreOption>(() => {
    const a = this.theme.tokens();
    const { slices } = this.model();
    return {
      animation: false,
      tooltip: {
        ...tooltipBase(a),
        trigger: "item",
        formatter: (params: unknown) => {
          const slice = slices[(params as { dataIndex: number }).dataIndex];
          if (!slice) return "";
          return tooltipHtml(a, slice.name, [
            { color: slice.color, label: "Importo", value: formatCurrency(slice.value), emphasis: true },
            { label: "Quota del totale", value: formatPercent(slice.share) },
          ]);
        },
      },
      series: [
        {
          type: "pie",
          radius: ["62%", "96%"],
          startAngle: 90,
          clockwise: true,
          avoidLabelOverlap: false,
          label: { show: false },
          labelLine: { show: false },
          emphasis: { scale: false },
          itemStyle: { borderColor: a.surface, borderWidth: 2 },
          data: slices.map((s) => ({ value: s.value, name: s.name, itemStyle: { color: s.color, opacity: s.opacity } })),
        },
      ],
    };
  });
}
