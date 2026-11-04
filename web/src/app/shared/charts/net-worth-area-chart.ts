import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from "@angular/core";
import type { EChartsCoreOption } from "echarts/core";
import { NgxEchartsDirective } from "ngx-echarts";
import { ThemeService } from "../../core/theme/theme.service";
import { formatCurrency, formatPeriodLabel } from "../../core/utils/format.utilities";
import { ChartDataTable, ChartEmpty, ChartLegend, ChartView, LegendEntry, TooltipRow, tooltipBase, tooltipHtml } from "./chart-parts";
import { asParams, categoryAxis, chartGrid, mergeByPeriod, valueAxis, verticalGradient } from "./chart-options";

export interface AmountPoint {
  period: string;
  amount: number;
}

export interface LineSeries {
  key: string;
  name: string;
  color: string;
  values: AmountPoint[];
}

const TOTAL_KEY = "amount";

/** Andamento del patrimonio: area con linea sottile, con eventuali linee per conto attivabili dalla legenda. */
@Component({
  selector: "app-net-worth-area-chart",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgxEchartsDirective, ChartEmpty, ChartLegend, ChartDataTable],
  template: `
    @if (points().length === 0) {
      @if (isSpark()) {
        <div [style.height.px]="height()"></div>
      } @else {
        <app-chart-empty [height]="height()" />
      }
    } @else {
      @if (series().length > 0) {
        <app-chart-legend [entries]="legend()" [toggleable]="true" (entryToggle)="toggle($event)" />
      }
      @if (!isSpark() && view() === "table") {
        <app-chart-data-table [height]="height() + 40" [columns]="tableColumns()" [rows]="tableRows()" />
      } @else {
        <div echarts class="chart" [options]="options()" [style.height.px]="height()"></div>
      }
    }
  `,
  styles: `
    :host { display: block; }
    .chart { width: 100%; }
  `,
})
export class NetWorthAreaChart {
  readonly points = input.required<AmountPoint[]>();
  readonly height = input(300);
  /** "spark": senza assi, per la card hero; "full": con assi e griglia */
  readonly variant = input<"spark" | "full">("full");
  readonly color = input<string>();
  readonly label = input("Patrimonio");
  /** serie per conto sovrapponibili (solo "full"): nascoste di default, si attivano dalla legenda */
  readonly accountSeries = input<LineSeries[]>([]);
  /** solo "full": in tabella le colonne seguono i conti attivati nella legenda */
  readonly view = input<ChartView>("chart");

  private readonly theme = inject(ThemeService);
  // i conti partono nascosti: si tiene l'insieme di quelli mostrati
  private readonly shown = signal<Set<string>>(new Set());

  protected readonly isSpark = computed(() => this.variant() === "spark");
  protected readonly series = computed(() => (this.isSpark() ? [] : this.accountSeries()));
  private readonly stroke = computed(() => this.color() ?? this.theme.tokens().chart.series[0]);
  private readonly visibleSeries = computed(() => this.series().filter((s) => this.shown().has(s.key)));

  private readonly rows = computed(() =>
    mergeByPeriod([{ key: TOTAL_KEY, values: this.points() }, ...this.series()]),
  );

  protected readonly legend = computed<LegendEntry[]>(() => [
    { key: TOTAL_KEY, label: this.label(), color: this.stroke(), shape: "square" },
    ...this.series().map((s) => ({
      key: s.key,
      label: s.name,
      color: s.color,
      hidden: !this.shown().has(s.key),
      shape: "line" as const,
    })),
  ]);

  protected readonly tableColumns = computed(() => [
    { key: "period", label: "Periodo" },
    { key: TOTAL_KEY, label: this.label(), align: "right" as const },
    ...this.visibleSeries().map((s) => ({ key: s.key, label: s.name, align: "right" as const })),
  ]);

  protected readonly tableRows = computed(() => {
    const keys = [TOTAL_KEY, ...this.visibleSeries().map((s) => s.key)];
    return [...this.rows()].reverse().map((row) => ({
      period: formatPeriodLabel(row.period, true),
      ...Object.fromEntries(keys.map((k) => [k, typeof row[k] === "number" ? formatCurrency(row[k] as number) : "—"])),
    }));
  });

  protected readonly options = computed<EChartsCoreOption>(() => {
    const a = this.theme.tokens();
    const spark = this.isSpark();
    const stroke = this.stroke();
    const rows = this.rows();
    const visible = this.visibleSeries();
    const label = this.label();

    const values = [
      ...this.points().map((p) => p.amount),
      ...visible.flatMap((s) => s.values.map((v) => v.amount)),
    ];
    const min = Math.min(...values);
    const max = Math.max(...values);
    const padding = Math.max((max - min) * 0.12, 1);

    const valueOf = (key: string) => rows.map((r) => (typeof r[key] === "number" ? (r[key] as number) : null));

    return {
      animation: false,
      grid: spark ? { top: 6, right: 2, bottom: 2, left: 2 } : chartGrid,
      xAxis: { ...categoryAxis(a, rows.map((r) => r.period)), show: !spark, boundaryGap: false },
      yAxis: {
        ...valueAxis(a),
        show: !spark,
        min: min - padding,
        max: max + padding,
        // gli estremi calcolati non sono valori "tondi": niente etichette su min e max
        axisLabel: { ...valueAxis(a).axisLabel, showMinLabel: false, showMaxLabel: false },
      },
      tooltip: {
        ...tooltipBase(a),
        trigger: "axis",
        axisPointer: {
          type: "line",
          lineStyle: { color: spark ? "rgba(255,255,255,0.5)" : a.borderStrong, width: 1, type: "solid" },
        },
        formatter: (params: unknown) => {
          const row = rows[asParams(params)[0]?.dataIndex ?? 0];
          if (!row) return "";
          const items: TooltipRow[] = [];
          if (typeof row[TOTAL_KEY] === "number") {
            items.push({ color: stroke, label, value: formatCurrency(row[TOTAL_KEY] as number), emphasis: true });
          }
          visible
            .filter((s) => typeof row[s.key] === "number")
            .sort((x, y) => Number(row[y.key]) - Number(row[x.key]))
            .forEach((s) => items.push({ color: s.color, label: s.name, value: formatCurrency(Number(row[s.key])) }));
          return tooltipHtml(a, formatPeriodLabel(row.period, true), items);
        },
      },
      series: [
        {
          id: TOTAL_KEY,
          type: "line",
          smooth: true,
          data: valueOf(TOTAL_KEY),
          connectNulls: true,
          showSymbol: false,
          symbolSize: 9,
          lineStyle: { color: stroke, width: 2 },
          itemStyle: { color: stroke, borderColor: spark ? "transparent" : a.surface, borderWidth: 2 },
          areaStyle: { color: verticalGradient(stroke, spark ? 0.35 : 0.22) },
          emphasis: { disabled: true },
        },
        ...visible.map((s) => ({
          id: s.key,
          name: s.name,
          type: "line",
          smooth: true,
          data: valueOf(s.key),
          connectNulls: true,
          showSymbol: false,
          symbolSize: 8,
          lineStyle: { color: s.color, width: 1.75 },
          itemStyle: { color: s.color, borderColor: a.surface, borderWidth: 2 },
          emphasis: { disabled: true },
        })),
      ],
    };
  });

  protected toggle(key: string) {
    if (key === TOTAL_KEY) return;
    this.shown.update((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }
}
