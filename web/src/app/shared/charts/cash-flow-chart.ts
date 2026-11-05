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

export interface CashFlowPoint {
  period: string;
  earned: number;
  spent: number;
  /** parte di `spent` investita (categoria investimenti) */
  invested: number;
}

interface Row extends CashFlowPoint {
  net: number;
  /** Tasso di risparmio (% delle entrate); null se nel periodo non ci sono entrate. */
  rate: number | null;
  /** Quota delle entrate investita; null se nel periodo non ci sono entrate. */
  investedRate: number | null;
}

/**
 * Flusso di cassa per periodo: entrate sopra lo zero, uscite sotto (la quota investita in fondo, con colore
 * proprio), saldo netto come linea. Un solo asse (euro) per tutte le grandezze.
 */
@Component({
  selector: "app-cash-flow-chart",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgxEchartsDirective, ChartEmpty, ChartLegend, ChartDataTable],
  template: `
    @if (isEmpty()) {
      <app-chart-empty [height]="height()" />
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
export class CashFlowChart {
  readonly points = input.required<CashFlowPoint[]>();
  readonly height = input(300);
  readonly view = input<ChartView>("chart");

  private readonly theme = inject(ThemeService);

  private readonly rows = computed<Row[]>(() =>
    this.points().map((p) => ({
      ...p,
      net: p.earned - p.spent,
      rate: p.earned > 0 ? ((p.earned - p.spent) / p.earned) * 100 : null,
      investedRate: p.earned > 0 ? (p.invested / p.earned) * 100 : null,
    })),
  );

  protected readonly isEmpty = computed(
    () => this.rows().length === 0 || this.rows().every((r) => r.earned === 0 && r.spent === 0),
  );

  protected readonly tableColumns = [
    { key: "period", label: "Periodo" },
    { key: "earned", label: "Entrate", align: "right" as const },
    { key: "spent", label: "Uscite", align: "right" as const },
    { key: "invested", label: "di cui investite", align: "right" as const },
    { key: "net", label: "Saldo", align: "right" as const },
  ];

  protected readonly tableRows = computed(() =>
    [...this.rows()].reverse().map((r) => ({
      period: formatPeriodLabel(r.period, true),
      earned: formatCurrency(r.earned),
      spent: formatCurrency(r.spent),
      invested: formatCurrency(r.invested),
      net: formatSignedCurrency(r.net),
    })),
  );

  protected readonly legend = computed(() => {
    const a = this.theme.tokens();
    return [
      { key: "earned", label: "Entrate", color: a.income },
      { key: "spent", label: "Uscite", color: a.expense },
      { key: "invested", label: "Investimenti", color: a.chart.invested },
      { key: "net", label: "Saldo netto", color: a.chart.net, shape: "line" as const },
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
            { color: a.income, label: "Entrate", value: formatCurrency(row.earned) },
            { color: a.expense, label: "Uscite", value: formatCurrency(row.spent) },
            { color: a.chart.invested, label: "di cui investite", value: formatCurrency(row.invested) },
            { color: a.chart.net, label: "Saldo", value: formatSignedCurrency(row.net), emphasis: true },
            { label: "Tasso di risparmio", value: row.rate === null ? "—" : formatPercent(row.rate) },
            {
              label: "Tasso di investimento",
              value: row.investedRate === null ? "—" : formatPercent(row.investedRate),
            },
          ]);
        },
      },
      series: [
        {
          id: "earned",
          type: "bar",
          stack: "flow",
          barMaxWidth: 36,
          barCategoryGap: "28%",
          data: rows.map((r) => r.earned),
          itemStyle: { color: a.income, borderRadius: [4, 4, 0, 0] },
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
          stack: "flow",
          barMaxWidth: 36,
          // uscite non investite; la quota investita si impila sotto
          data: rows.map((r) => ({
            value: -(r.spent - r.invested),
            // barre negative: angoli arrotondati verso il basso solo se è il segmento più basso
            itemStyle: { borderRadius: r.invested > 0 ? 0 : [0, 0, 4, 4] },
          })),
          itemStyle: { color: a.expense },
          emphasis: { disabled: true },
        },
        {
          id: "invested",
          type: "bar",
          stack: "flow",
          barMaxWidth: 36,
          data: rows.map((r) => -r.invested),
          itemStyle: { color: a.chart.invested, borderRadius: [0, 0, 4, 4] },
          emphasis: { disabled: true },
        },
        {
          id: "net",
          type: "line",
          smooth: true,
          data: rows.map((r) => r.net),
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
