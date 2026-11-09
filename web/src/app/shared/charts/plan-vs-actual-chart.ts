import { ChangeDetectionStrategy, Component, computed, inject, input } from "@angular/core";
import type { EChartsCoreOption } from "echarts/core";
import { NgxEchartsDirective } from "ngx-echarts";
import { ThemeService } from "../../core/theme/theme.service";
import {
  formatCurrency,
  formatPercent,
  formatPeriodLabel,
  formatSignedCurrency,
} from "../../core/utils/format.utilities";
import { ChartDataTable, ChartEmpty, ChartLegend, ChartView, tooltipBase, tooltipHtml } from "./chart-parts";
import { asParams, categoryAxis, chartGrid, valueAxis } from "./chart-options";

export interface PlanVsActualChartPoint {
  period: string;
  planned: number;
  spent: number;
}

interface Row extends PlanVsActualChartPoint {
  /** speso − previsto: positivo = sforamento */
  diff: number;
  /** speso in % del previsto */
  ratio: number | null;
}

/**
 * Pianificazioni per periodo: previsto e speso affiancati (speso verde entro la previsione, rosso oltre),
 * scostamento (speso − previsto) come linea sullo stesso asse.
 */
@Component({
  selector: "app-plan-vs-actual-chart",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgxEchartsDirective, ChartEmpty, ChartLegend, ChartDataTable],
  template: `
    @if (rows().length === 0) {
      <app-chart-empty [height]="height()" message="Nessuna pianificazione nel periodo selezionato" />
    } @else if (view() === "table") {
      <app-chart-data-table [height]="height() + 34" [columns]="tableColumns" [rows]="tableRows()" />
    } @else {
      <app-chart-legend [entries]="legend()" />
      <div echarts class="chart" [options]="options()" [style.height.px]="height()"></div>
    }
  `,
  styles: `
    :host { display: block; }
    .chart { width: 100%; }
  `,
})
export class PlanVsActualChart {
  readonly points = input.required<PlanVsActualChartPoint[]>();
  readonly height = input(300);
  readonly view = input<ChartView>("chart");

  private readonly theme = inject(ThemeService);

  private readonly rows = computed<Row[]>(() =>
    this.points().map((p) => ({
      ...p,
      diff: p.spent - p.planned,
      ratio: p.planned > 0 ? (p.spent / p.planned) * 100 : null,
    })),
  );

  protected readonly tableColumns = [
    { key: "period", label: "Periodo" },
    { key: "planned", label: "Previsto", align: "right" as const },
    { key: "spent", label: "Speso", align: "right" as const },
    { key: "diff", label: "Scostamento", align: "right" as const },
    { key: "ratio", label: "% previsto", align: "right" as const },
  ];

  protected readonly tableRows = computed(() =>
    [...this.rows()].reverse().map((r) => ({
      period: formatPeriodLabel(r.period, true),
      planned: formatCurrency(r.planned),
      spent: formatCurrency(r.spent),
      diff: formatSignedCurrency(r.diff),
      ratio: r.ratio === null ? "—" : formatPercent(r.ratio),
    })),
  );

  protected readonly legend = computed(() => {
    const a = this.theme.tokens();
    return [
      { key: "planned", label: "Previsto", color: a.chart.other },
      { key: "within", label: "Speso entro la previsione", color: a.income },
      { key: "over", label: "Speso oltre la previsione", color: a.expense },
      { key: "diff", label: "Scostamento", color: a.chart.net, shape: "line" as const },
    ];
  });

  protected readonly options = computed<EChartsCoreOption>(() => {
    const a = this.theme.tokens();
    const rows = this.rows();

    return {
      animation: false,
      grid: chartGrid,
      xAxis: categoryAxis(a, rows.map((r) => r.period), false),
      yAxis: valueAxis(a),
      tooltip: {
        ...tooltipBase(a),
        trigger: "axis",
        axisPointer: { type: "shadow", shadowStyle: { color: a.surfaceAlt, opacity: 0.7 } },
        formatter: (params: unknown) => {
          const row = rows[asParams(params)[0]?.dataIndex ?? 0];
          if (!row) return "";
          return tooltipHtml(a, formatPeriodLabel(row.period, true), [
            { color: a.chart.other, label: "Previsto", value: formatCurrency(row.planned) },
            { color: row.diff > 0 ? a.expense : a.income, label: "Speso", value: formatCurrency(row.spent) },
            { color: a.chart.net, label: "Scostamento", value: formatSignedCurrency(row.diff), emphasis: true },
            { label: "Speso / previsto", value: row.ratio === null ? "—" : formatPercent(row.ratio) },
          ]);
        },
      },
      series: [
        {
          id: "planned",
          type: "bar",
          barMaxWidth: 28,
          barGap: "12%",
          barCategoryGap: "32%",
          data: rows.map((r) => r.planned),
          itemStyle: { color: a.chart.other, borderRadius: [4, 4, 0, 0] },
          emphasis: { disabled: true },
          markLine: {
            silent: true,
            symbol: "none",
            label: { show: false },
            lineStyle: { color: a.borderStrong, type: "solid", width: 1 },
            data: [{ yAxis: 0 }],
          },
        },
        {
          id: "spent",
          type: "bar",
          barMaxWidth: 28,
          data: rows.map((r) => ({ value: r.spent, itemStyle: { color: r.diff > 0 ? a.expense : a.income } })),
          itemStyle: { borderRadius: [4, 4, 0, 0] },
          emphasis: { disabled: true },
        },
        {
          id: "diff",
          type: "line",
          smooth: true,
          data: rows.map((r) => r.diff),
          symbol: "circle",
          symbolSize: 7,
          lineStyle: { color: a.chart.net, width: 2 },
          itemStyle: { color: a.chart.net, borderColor: a.surface, borderWidth: 2 },
          emphasis: { disabled: true },
          z: 3,
        },
      ],
    };
  });
}
