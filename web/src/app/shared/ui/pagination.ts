import { ChangeDetectionStrategy, Component, computed, input, output } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";

/** Paginazione compatta a numeri (pagine 0-based in ingresso e in uscita). */
@Component({
  selector: "app-pagination",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule],
  template: `
    <nav aria-label="Paginazione">
      <button type="button" [disabled]="page() === 0" (click)="pageChange.emit(page() - 1)" aria-label="Pagina precedente">
        <mat-icon class="icon-sm">chevron_left</mat-icon>
      </button>
      @for (item of items(); track $index) {
        @if (item === null) {
          <span class="ellipsis">…</span>
        } @else {
          <button
            type="button"
            [class.active]="item === page()"
            [attr.aria-current]="item === page() ? 'page' : null"
            (click)="pageChange.emit(item)"
          >
            {{ item + 1 }}
          </button>
        }
      }
      <button
        type="button"
        [disabled]="page() >= pageCount() - 1"
        (click)="pageChange.emit(page() + 1)"
        aria-label="Pagina successiva"
      >
        <mat-icon class="icon-sm">chevron_right</mat-icon>
      </button>
    </nav>
  `,
  styles: `
    nav { display: flex; gap: 4px; align-items: center; justify-content: center; }
    button {
      min-width: 32px; height: 32px; padding: 0 8px; border: none; background: transparent; cursor: pointer;
      border-radius: var(--app-radius-option); color: var(--app-text); font-size: var(--app-text-md);
      display: grid; place-items: center;
    }
    button:hover:not(:disabled) { background-color: var(--app-hover); }
    button:disabled { opacity: 0.38; cursor: default; }
    button.active { background-color: var(--app-primary-soft); color: var(--app-primary); font-weight: 600; }
    .ellipsis { min-width: 24px; text-align: center; color: var(--app-text-muted); }
  `,
})
export class Pagination {
  readonly page = input.required<number>();
  readonly pageCount = input.required<number>();
  readonly pageChange = output<number>();

  protected readonly items = computed<(number | null)[]>(() => {
    const count = this.pageCount();
    const current = this.page();
    if (count <= 7) return Array.from({ length: count }, (_, i) => i);
    const pages = new Set([0, count - 1, current - 1, current, current + 1].filter((p) => p >= 0 && p < count));
    const sorted = [...pages].sort((a, b) => a - b);
    const result: (number | null)[] = [];
    sorted.forEach((p, i) => {
      if (i > 0 && p - sorted[i - 1] > 1) result.push(null);
      result.push(p);
    });
    return result;
  });
}
