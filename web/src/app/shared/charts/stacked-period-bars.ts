import { ChangeDetectionStrategy, Component, computed, inject, input, output } from "@angular/core";
import type { ECharts, EChartsCoreOption } from "echarts/core";
import { NgxEchartsDirective } from "ngx-echarts";
import { ThemeService } from "../../core/theme/theme.service";
import { formatCurrency, formatPeriodLabel } from "../../core/utils/format.utilities";
import { ChartDataTable, ChartEmpty, ChartLegend, ChartView, TooltipRow, tooltipBase, tooltipHtml } from "./chart-parts";
import { PeriodClick, PeriodRow, asParams, bindPeriodClick, categoryAxis, chartGrid, valueAxis } from "./chart-options";

export interface StackSeries {
  key: string;
  name: string;
  values: { period: string; value: number }[];
}

const OTHER_KEY = "__other__";

/**
 * Barre impilate per periodo (composizione delle spese per categoria nel tempo).
 * Le serie sono ordinate per totale e oltre `maxSeries` raggruppate in "Altre".
 * Oltre la palette i colori ripartono con opacità ridotta, così ogni serie resta distinguibile dalle vicine.
 */
@Component({
  selector: "app-stacked-period-bars",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgxEchartsDirective, ChartEmpty, ChartLegend, ChartDataTable],
  template: `
    @if (model().rows.length === 0) {
      <app-chart-empty [height]="height()" />
    } @else if (view() === "table") {
      <app-chart-data-table [height]="height() + 40" [columns]="tableColumns()" [rows]="tableRows()" />
    } @else {
      <app-chart-legend [entries]="legend()" />
      <div echarts class="chart" [options]="options()" [style.height.px]="height()" (chartInit)="bindClicks($event)"></div>
    }
  `,
  styles: `
    :host { display: block; }
    .chart { width: 100%; }
  `,
})
export class StackedPeriodBars {
  readonly series = input.required<StackSeries[]>();
  /** serie mostrate con un proprio colore (inclusa "Altre"); `Infinity` mostra tutte le serie */
  readonly maxSeries = input(6);
  readonly height = input(320);
  readonly view = input<ChartView>("chart");
  /** click su un periodo: `seriesKey` della serie cliccata, null a vuoto nella colonna o su "Altre" */
  readonly periodClick = output<PeriodClick>();

  private readonly theme = inject(ThemeService);

  protected bindClicks(chart: ECharts) {
    bindPeriodClick(
      chart,
      (index) => this.model().rows[index]?.period,
      (click) => this.periodClick.emit(click.seriesKey === OTHER_KEY ? { ...click, seriesKey: null } : click),
    );
  }

  protected readonly model = computed(() => {
    const a = this.theme.tokens();
    const maxSeries = this.maxSeries();
    const ranked = this.series()
      .map((s) => ({ ...s, total: s.values.reduce((sum, p) => sum + p.value, 0) }))
      .filter((s) => s.total > 0)
      .sort((x, y) => y.total - x.total);

    const head = ranked.length > maxSeries ? ranked.slice(0, maxSeries - 1) : ranked;
    const tail = ranked.slice(head.length);

    const map = new Map<string, PeriodRow>();
    const add = (period: string, key: string, value: number) => {
      const row = map.get(period) ?? { period };
      row[key] = (Number(row[key]) || 0) + value;
      map.set(period, row);
    };
    head.forEach((s) => s.values.forEach((p) => add(p.period, s.key, p.value)));
    tail.forEach((s) => s.values.forEach((p) => add(p.period, OTHER_KEY, p.value)));

    const palette = a.chart.series;
    const keys = [
      ...head.map((s, i) => ({
        key: s.key,
        name: s.name,
        color: palette.length ? palette[i % palette.length] : a.chart.other,
        opacity: Math.max(0.3, 1 - (palette.length ? Math.floor(i / palette.length) : 0) * 0.4),
      })),
      ...(tail.length ? [{ key: OTHER_KEY, name: `Altre (${tail.length})`, color: a.chart.other, opacity: 1 }] : []),
    ];

    return { rows: [...map.values()].sort((x, y) => x.period.localeCompare(y.period)), keys };
  });

  protected readonly legend = computed(() =>
    this.model().keys.map((k) => ({ key: k.key, label: k.name, color: k.color, opacity: k.opacity })),
  );

  protected readonly tableColumns = computed(() => [
    { key: "period", label: "Periodo" },
    ...this.model().keys.map((k) => ({ key: k.key, label: k.name, align: "right" as const })),
  ]);

  protected readonly tableRows = computed(() => {
    const { rows, keys } = this.model();
    return [...rows].reverse().map((row) => ({
      period: formatPeriodLabel(row.period, true),
      ...Object.fromEntries(keys.map((k) => [k.key, formatCurrency(Number(row[k.key]) || 0)])),
    }));
  });

  protected readonly options = computed<EChartsCoreOption>(() => {
    const a = this.theme.tokens();
    const { rows, keys } = this.model();

    return {
      animation: false,
      grid: chartGrid,
      xAxis: categoryAxis(a, rows.map((r) => r.period)),
      yAxis: valueAxis(a),
      tooltip: {
        ...tooltipBase(a),
        trigger: "axis",
        axisPointer: { type: "shadow", shadowStyle: { color: a.surfaceAlt, opacity: 0.7 } },
        formatter: (params: unknown) => {
          const row = rows[asParams(params)[0]?.dataIndex ?? 0];
          if (!row) return "";
          const items: TooltipRow[] = [...keys]
            .reverse()
            .filter((k) => typeof row[k.key] === "number")
            .map((k) => ({ color: k.color, opacity: k.opacity, label: k.name, value: formatCurrency(Number(row[k.key]) || 0) }));
          const total = keys.reduce((sum, k) => sum + (Number(row[k.key]) || 0), 0);
          items.push({ label: "Totale", value: formatCurrency(total), emphasis: true });
          return tooltipHtml(a, formatPeriodLabel(row.period, true), items);
        },
      },
      series: keys.map((k, i) => ({
        id: k.key,
        name: k.name,
        type: "bar",
        stack: "stack",
        barMaxWidth: 44,
        barCategoryGap: "24%",
        data: rows.map((r) => (typeof r[k.key] === "number" ? r[k.key] : 0)),
        itemStyle: {
          color: k.color,
          opacity: k.opacity,
          borderColor: a.surface,
          borderWidth: 1,
          borderRadius: i === keys.length - 1 ? [4, 4, 0, 0] : 0,
        },
        emphasis: { disabled: true },
      })),
    };
  });
}
