import { NgTemplateOutlet } from "@angular/common";
import { ChangeDetectionStrategy, Component, TemplateRef, computed, contentChild, input, output } from "@angular/core";
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
 */
@Component({
  selector: "app-data-table",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgTemplateOutlet, MatPaginatorModule, MatIconModule, EmptyState],
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
                <tr class="data-row" [class.clickable]="clickableRows()">
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
export class DataTable<T> {
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

  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();
  readonly sortChange = output<string>();

  protected readonly rowTemplate = contentChild.required<TemplateRef<DataTableRowContext<T>>>(TemplateRef);
  protected readonly skeletonRows = computed(() => Array.from({ length: Math.min(this.pageSize(), 10) }));

  protected onPage(event: PageEvent) {
    if (event.pageSize !== this.pageSize()) {
      this.pageSizeChange.emit(event.pageSize);
    } else {
      this.pageChange.emit(event.pageIndex);
    }
  }
}
