import { NgTemplateOutlet } from "@angular/common";
import { ChangeDetectionStrategy, Component, TemplateRef, computed, contentChild, input, output } from "@angular/core";
import { MatCheckbox } from "@angular/material/checkbox";
import { MatIconModule } from "@angular/material/icon";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { EmptyState } from "./empty-state";

export interface DataTableColumn {
  id: string;
  label: string;
  /** larghezza della colonna (es. "12%") */
  minWidth?: string;
  align?: "left" | "right" | "center";
  /** se true l'intestazione è cliccabile ed emette sortChange */
  sortable?: boolean;
}

export interface DataTableRowContext<T> {
  $implicit: T;
  index: number;
}

/**
 * Tabella paginata condivisa (movimenti, categorie): righe a filo con separatori sottili.
 * Le celle di ogni riga arrivano da un `<ng-template let-row>` proiettato che contiene i `<td>`;
 * l'azione con l'attributo `empty-action` viene mostrata nello stato vuoto.
 * Con `rowKey` compare una colonna di checkbox per la selezione multipla delle righe visibili (`selection`/`selectionChange`).
 */
@Component({
  selector: "app-data-table",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgTemplateOutlet, MatPaginatorModule, MatIconModule, MatCheckbox, EmptyState],
  host: { class: "surface" },
  template: `
    @if (isEmpty() && !isLoading()) {
      <app-empty-state icon="inbox" [title]="emptyMessage()" [text]="emptySubtext()" [minHeight]="320">
        <ng-content select="[empty-action]" />
      </app-empty-state>
    } @else {
      <div class="viewport" [style.max-height.px]="viewportHeight()">
        <table class="data-table" [style.min-width.px]="minTableWidth()" [style.table-layout]="tableLayout()">
          <thead>
            <tr>
              @if (selectable()) {
                <th class="select-cell">
                  <mat-checkbox
                    [checked]="allSelected()"
                    [indeterminate]="someSelected()"
                    [disabled]="isLoading() || rows().length === 0"
                    (change)="toggleAll()"
                    aria-label="Seleziona tutte le righe della pagina"
                  />
                </th>
              }
              @for (column of columns(); track column.id) {
                <th
                  [style.width]="column.minWidth"
                  [style.text-align]="column.align ?? 'left'"
                  [attr.aria-sort]="sortBy() === column.id ? (sortDirection() === 'asc' ? 'ascending' : 'descending') : null"
                >
                  @if (column.sortable && sortable()) {
                    <button
                      type="button"
                      class="sort-label"
                      [class.active]="sortBy() === column.id"
                      (click)="sortChange.emit(column.id)"
                    >
                      {{ column.label }}
                      <mat-icon class="icon-xs sort-icon">
                        {{ sortBy() === column.id && sortDirection() === "asc" ? "arrow_upward" : "arrow_downward" }}
                      </mat-icon>
                    </button>
                  } @else {
                    {{ column.label }}
                  }
                </th>
              }
            </tr>
          </thead>
          <tbody>
            @if (isLoading()) {
              @for (placeholder of skeletonRows(); track $index) {
                <tr>
                  @if (selectable()) {
                    <td class="select-cell"></td>
                  }
                  @for (column of columns(); track column.id) {
                    <td [style.text-align]="column.align ?? 'left'">
                      <span
                        class="skeleton skeleton-text"
                        [style.width]="column.id === 'description' ? '70%' : '55%'"
                        [style.margin-left]="column.align === 'right' ? 'auto' : null"
                      ></span>
                    </td>
                  }
                </tr>
              }
            } @else {
              @for (row of rows(); track $index) {
                <tr class="data-row" [class.clickable]="clickableRows()" [class.selected]="selectable() && isSelected(row)">
                  @if (selectable()) {
                    <td class="select-cell">
                      <mat-checkbox
                        [checked]="isSelected(row)"
                        (change)="toggleRow(row)"
                        (click)="$event.stopPropagation()"
                        aria-label="Seleziona la riga"
                      />
                    </td>
                  }
                  <ng-container *ngTemplateOutlet="rowTemplate(); context: { $implicit: row, index: $index }" />
                </tr>
              }
            }
          </tbody>
        </table>
      </div>
    }

    @if (!isEmpty()) {
      <mat-paginator
        class="paginator"
        [length]="totalCount()"
        [pageIndex]="page()"
        [pageSize]="pageSize()"
        [pageSizeOptions]="pageSizeOptions()"
        (page)="onPage($event)"
      />
    }
  `,
  styles: `
    :host { width: 100%; overflow: hidden; display: flex; flex-direction: column; }
    .viewport { overflow-x: auto; }
    .data-table .select-cell { width: 52px; padding-right: 0; }
    .data-table .select-cell .mat-mdc-checkbox { margin: -10px 0 -10px -10px; }
    .paginator { border-top: 1px solid var(--app-border); }
    .sort-label {
      display: inline-flex; align-items: center; gap: 4px; border: none; background: none; padding: 0; cursor: pointer;
      font: inherit; color: inherit; text-transform: inherit; letter-spacing: inherit;
    }
    .sort-label .sort-icon { opacity: 0; transition: opacity .15s; }
    .sort-label:hover .sort-icon { opacity: 0.5; }
    .sort-label.active { color: var(--app-text); }
    .sort-label.active .sort-icon { opacity: 1; }
  `,
})
export class DataTable<T, K = unknown> {
  readonly columns = input.required<DataTableColumn[]>();
  readonly rows = input.required<T[]>();
  readonly isLoading = input(false);
  readonly isEmpty = input(false);
  readonly emptyMessage = input("Nessun dato trovato");
  readonly emptySubtext = input("");
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly pageSizeOptions = input([10, 25, 100]);
  readonly minTableWidth = input(700);
  /** "fixed" rispetta le larghezze delle colonne e manda a capo il contenuto invece di allargare la tabella */
  readonly tableLayout = input<"auto" | "fixed">("auto");
  readonly viewportHeight = input<number | undefined>();
  readonly sortBy = input<string | undefined>();
  readonly sortDirection = input<"asc" | "desc">("asc");
  readonly sortable = input(false);
  readonly clickableRows = input(false);
  /** chiave di riga per la selezione multipla; se assente la colonna di selezione non compare */
  readonly rowKey = input<(row: T) => K>();
  readonly selection = input<ReadonlySet<K>>(new Set<K>());

  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();
  readonly sortChange = output<string>();
  readonly selectionChange = output<ReadonlySet<K>>();

  protected readonly rowTemplate = contentChild.required<TemplateRef<DataTableRowContext<T>>>(TemplateRef);
  protected readonly skeletonRows = computed(() => Array.from({ length: Math.min(this.pageSize(), 10) }));

  protected readonly selectable = computed(() => this.rowKey() !== undefined);
  private readonly rowKeys = computed(() => {
    const key = this.rowKey();
    return key ? this.rows().map(key) : [];
  });
  protected readonly allSelected = computed(() => {
    const keys = this.rowKeys();
    const selection = this.selection();
    return keys.length > 0 && keys.every((k) => selection.has(k));
  });
  protected readonly someSelected = computed(() => !this.allSelected() && this.rowKeys().some((k) => this.selection().has(k)));

  protected isSelected(row: T) {
    const key = this.rowKey();
    return key ? this.selection().has(key(row)) : false;
  }

  protected toggleRow(row: T) {
    const key = this.rowKey();
    if (!key) return;
    const next = new Set(this.selection());
    const k = key(row);
    if (next.has(k)) next.delete(k);
    else next.add(k);
    this.selectionChange.emit(next);
  }

  // tutte selezionate: deseleziona le righe visibili; altrimenti le aggiunge alla selezione
  protected toggleAll() {
    const next = new Set(this.selection());
    const keys = this.rowKeys();
    if (this.allSelected()) keys.forEach((k) => next.delete(k));
    else keys.forEach((k) => next.add(k));
    this.selectionChange.emit(next);
  }

  protected onPage(event: PageEvent) {
    if (event.pageSize !== this.pageSize()) {
      this.pageSizeChange.emit(event.pageSize);
    } else {
      this.pageChange.emit(event.pageIndex);
    }
  }
}
