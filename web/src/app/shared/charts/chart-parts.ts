import { NgTemplateOutlet } from "@angular/common";
import { ChangeDetectionStrategy, Component, input, model, output } from "@angular/core";
import { MatButtonToggleModule } from "@angular/material/button-toggle";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltip } from "@angular/material/tooltip";
import { AppTokens } from "../../core/theme/theme.service";

/* ---------- tooltip HTML per ECharts ---------- */

export interface TooltipRow {
  color?: string;
  opacity?: number;
  label: string;
  value: string;
  emphasis?: boolean;
}

const escapeHtml = (value: string) =>
  value.replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c] ?? c);

/** Contenuto del tooltip dei grafici: titolo e righe colore · etichetta · valore. */
export const tooltipHtml = (a: AppTokens, title: string, rows: TooltipRow[]) => {
  const rowHtml = rows
    .map(
      (r) => `
      <div style="display:flex;align-items:center;gap:8px;margin-top:4px">
        <span style="width:8px;height:8px;border-radius:2px;flex-shrink:0;background:${r.color ?? "transparent"};opacity:${r.opacity ?? 1}"></span>
        <span style="font-size:var(--app-text-sm);color:${a.textSecondary};flex:1">${escapeHtml(r.label)}</span>
        <span style="font-size:var(--app-text-sm);font-weight:${r.emphasis ? 700 : 600};color:${a.text};font-variant-numeric:tabular-nums;margin-left:16px">${escapeHtml(r.value)}</span>
      </div>`,
    )
    .join("");
  return `<div style="min-width:180px;font-family:${a.fontFamily.replace(/"/g, "'")}">
    <div style="font-size:var(--app-text-sm);font-weight:600;color:${a.textSecondary};margin-bottom:2px">${escapeHtml(title)}</div>
    ${rowHtml}
  </div>`;
};

/** Opzioni comuni del tooltip ECharts nello stile dell'app. */
export const tooltipBase = (a: AppTokens) => ({
  backgroundColor: a.surface,
  borderColor: a.borderStrong,
  borderWidth: 1,
  padding: [10, 12],
  extraCssText: `border-radius:${a.radiusControl + 2}px;box-shadow:${
    a.mode === "dark" ? "0 12px 32px rgba(0,0,0,0.5)" : "0 12px 28px rgba(15,23,42,0.14)"
  };`,
  confine: true,
});

/* ---------- legenda ---------- */

export interface LegendEntry {
  key: string;
  label: string;
  color: string;
  /** opacità del marcatore, per le serie che riusano un colore della palette */
  opacity?: number;
  hidden?: boolean;
  /** forma del marcatore: quadrato per barre/aree, linea per serie a linea */
  shape?: "square" | "line";
}

/** Legenda sopra il grafico; se `toggleable` le voci mostrano/nascondono la serie (evento `toggle`). */
@Component({
  selector: "app-chart-legend",
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @for (entry of entries(); track entry.key) {
      @if (toggleable()) {
        <button type="button" class="entry clickable" [attr.aria-pressed]="!entry.hidden" (click)="entryToggle.emit(entry.key)">
          <ng-container *ngTemplateOutlet="content; context: { $implicit: entry }" />
        </button>
      } @else {
        <span class="entry"><ng-container *ngTemplateOutlet="content; context: { $implicit: entry }" /></span>
      }
    }
    <ng-template #content let-entry>
      <span
        class="marker"
        [class.line]="entry.shape === 'line'"
        [style.background-color]="entry.hidden ? 'transparent' : entry.color"
        [style.border]="entry.hidden ? '1.5px solid ' + entry.color : 'none'"
        [style.opacity]="entry.opacity ?? 1"
      ></span>
      <span class="label" [class.hidden]="entry.hidden">{{ entry.label }}</span>
    </ng-template>
  `,
  imports: [NgTemplateOutlet],
  styles: `
    :host { display: flex; flex-wrap: wrap; gap: 4px; margin-bottom: 12px; }
    .entry {
      display: inline-flex; align-items: center; gap: 8px; padding: 2px 8px; border: none; background: none; font: inherit;
      border-radius: var(--app-radius-control);
    }
    .entry.clickable { cursor: pointer; }
    .entry.clickable:hover { background-color: var(--app-surface-alt); }
    .marker { width: 9px; height: 9px; border-radius: 2px; box-sizing: border-box; }
    .marker.line { width: 14px; height: 3px; border-radius: 2px; }
    .label { font-size: var(--app-text-sm); color: var(--app-text-secondary); white-space: nowrap; }
    .label.hidden { color: var(--app-text-muted); text-decoration: line-through; }
  `,
})
export class ChartLegend {
  readonly entries = input.required<LegendEntry[]>();
  readonly toggleable = input(false);
  readonly entryToggle = output<string>();
}

/* ---------- switch grafico/tabella ---------- */

export type ChartView = "chart" | "table";

/** Switch grafico/tabella: ogni grafico ha una vista tabellare equivalente. */
@Component({
  selector: "app-chart-view-toggle",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonToggleModule, MatIconModule, MatTooltip],
  template: `
    <mat-button-toggle-group
      class="segmented"
      [value]="value()"
      (change)="value.set($event.value)"
      aria-label="Vista"
      hideSingleSelectionIndicator
    >
      <mat-button-toggle value="chart" aria-label="Grafico" matTooltip="Grafico">
        <mat-icon class="icon-sm">bar_chart</mat-icon>
      </mat-button-toggle>
      <mat-button-toggle value="table" aria-label="Tabella" matTooltip="Tabella">
        <mat-icon class="icon-sm">table_rows</mat-icon>
      </mat-button-toggle>
    </mat-button-toggle-group>
  `,
  styles: `
    .mat-icon { vertical-align: -4px; }
    :host ::ng-deep .mat-button-toggle-appearance-standard .mat-button-toggle-label-content { padding: 0 8px; }
  `,
})
export class ChartViewToggle {
  readonly value = model<ChartView>("chart");
}

/* ---------- tabella equivalente ---------- */

export interface ChartTableColumn {
  key: string;
  label: string;
  align?: "left" | "right";
}

@Component({
  selector: "app-chart-data-table",
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { "[style.max-height.px]": "height()", "[style.min-height.px]": "minHeight()" },
  template: `
    <table>
      <thead>
        <tr>
          @for (column of columns(); track column.key) {
            <th [style.text-align]="column.align ?? 'left'">{{ column.label }}</th>
          }
        </tr>
      </thead>
      <tbody>
        @for (row of rows(); track $index) {
          <tr>
            @for (column of columns(); track column.key) {
              <td [style.text-align]="column.align ?? 'left'">{{ row[column.key] }}</td>
            }
          </tr>
        }
      </tbody>
    </table>
  `,
  styles: `
    :host { display: block; overflow: auto; }
    table { width: 100%; border-collapse: separate; border-spacing: 0; }
    th {
      position: sticky; top: 0; background-color: var(--app-surface); padding: 8px 16px; white-space: nowrap;
      border-bottom: 1px solid var(--app-border); color: var(--app-text-muted); font-size: var(--app-text-sm); font-weight: 600;
      text-transform: uppercase; letter-spacing: 0.08em;
    }
    td {
      padding: 8px 16px; border-bottom: 1px solid var(--app-border); font-size: var(--app-text-md);
      font-variant-numeric: tabular-nums; color: var(--app-text);
    }
    tbody tr:hover { background-color: var(--app-hover); }
  `,
})
export class ChartDataTable {
  readonly columns = input.required<ChartTableColumn[]>();
  readonly rows = input.required<Record<string, string>[]>();
  readonly height = input(300);
  protected readonly minHeight = () => Math.min(this.height(), 200);
}

/* ---------- stato vuoto ---------- */

@Component({
  selector: "app-chart-empty",
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { "[style.height.px]": "height()" },
  template: `{{ message() }}`,
  styles: `
    :host {
      display: grid; place-items: center; color: var(--app-text-muted); font-size: var(--app-text-md);
      border: 1px dashed var(--app-border); border-radius: var(--app-radius-control); text-align: center; padding: 8px;
    }
  `,
})
export class ChartEmpty {
  readonly height = input(260);
  readonly message = input("Nessun dato per il periodo selezionato");
}
