import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from "@angular/core";
import type { EChartsCoreOption } from "echarts/core";
import { NgxEchartsDirective } from "ngx-echarts";
import { ThemeService } from "../../core/theme/theme.service";
import { formatCurrency, formatPeriodLabel } from "../../core/utils/format.utilities";
import { ChartDataTable, ChartEmpty, ChartLegend, ChartView, tooltipBase, tooltipHtml } from "./chart-parts";
import { asParams, categoryAxis, chartGrid, mergeByPeriod, valueAxis } from "./chart-options";
import { LineSeries } from "./net-worth-area-chart";

/** Più serie nello stesso asse (saldo per conto). La legenda mostra/nasconde le singole linee. */
@Component({
  selector: "app-balance-lines-chart",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgxEchartsDirective, ChartEmpty, ChartLegend, ChartDataTable],
  template: `
    @if (rows().length === 0) {
      <app-chart-empty [height]="height()" />
    } @else if (view() === "table") {
      <app-chart-data-table [height]="height() + 40" [columns]="tableColumns()" [rows]="tableRows()" />
    } @else {
      <app-chart-legend [entries]="legend()" [toggleable]="true" (entryToggle)="toggle($event)" />
      <div echarts class="chart" [options]="options()" [style.height.px]="height()"></div>
    }
  `,
  styles: `
    :host { display: block; }
    .chart { width: 100%; }
  `,
})
export class BalanceLinesChart {
  readonly series = input.required<LineSeries[]>();
  readonly height = input(320);
  readonly view = input<ChartView>("chart");

  private readonly theme = inject(ThemeService);
  private readonly hidden = signal<Set<string>>(new Set());

  protected readonly rows = computed(() => mergeByPeriod(this.series()));

  protected readonly legend = computed(() =>
    this.series().map((s) => ({
      key: s.key,
      label: s.name,
      color: s.color,
      hidden: this.hidden().has(s.key),
      shape: "line" as const,
    })),
  );

  protected readonly tableColumns = computed(() => [
    { key: "period", label: "Periodo" },
    ...this.series().map((s) => ({ key: s.key, label: s.name, align: "right" as const })),
  ]);

  protected readonly tableRows = computed(() =>
    [...this.rows()].reverse().map((row) => ({
      period: formatPeriodLabel(row.period, true),
      ...Object.fromEntries(
        this.series().map((s) => [s.key, typeof row[s.key] === "number" ? formatCurrency(row[s.key] as number) : "—"]),
      ),
    })),
  );

  protected readonly options = computed<EChartsCoreOption>(() => {
    const a = this.theme.tokens();
    const rows = this.rows();
    const visible = this.series().filter((s) => !this.hidden().has(s.key));

    return {
      animation: false,
      grid: chartGrid,
      xAxis: { ...categoryAxis(a, rows.map((r) => r.period)), boundaryGap: false },
      yAxis: { ...valueAxis(a), scale: true },
      tooltip: {
        ...tooltipBase(a),
        trigger: "axis",
        axisPointer: { type: "line", lineStyle: { color: a.borderStrong, width: 1, type: "solid" } },
        formatter: (params: unknown) => {
          const row = rows[asParams(params)[0]?.dataIndex ?? 0];
          if (!row) return "";
          const items = visible
            .filter((s) => typeof row[s.key] === "number")
            .sort((x, y) => Number(row[y.key]) - Number(row[x.key]))
            .map((s) => ({ color: s.color, label: s.name, value: formatCurrency(Number(row[s.key])) }));
          return tooltipHtml(a, formatPeriodLabel(row.period, true), items);
        },
      },
      series: visible.map((s) => ({
        id: s.key,
        name: s.name,
        type: "line",
        smooth: true,
        connectNulls: true,
        showSymbol: false,
        symbolSize: 9,
        data: rows.map((r) => (typeof r[s.key] === "number" ? r[s.key] : null)),
        lineStyle: { color: s.color, width: 2 },
        itemStyle: { color: s.color, borderColor: a.surface, borderWidth: 2 },
        emphasis: { disabled: true },
      })),
    };
  });

  protected toggle(key: string) {
    this.hidden.update((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }
}
