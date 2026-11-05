import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal, untracked } from "@angular/core";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { CategoriesStore } from "../../core/state/categories.store";
import { toDateInputValue } from "../../core/utils/date.utilities";
import { AppModal } from "../../shared/ui/app-modal";

const createInitialRange = () => {
  const today = new Date();
  return {
    start: toDateInputValue(new Date(today.getFullYear(), 0, 1)),
    end: toDateInputValue(today),
  };
};

/** Riapplica le parole chiave delle categorie ai movimenti già salvati in un periodo. */
@Component({
  selector: "app-category-sync-modal",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AppModal, MatFormFieldModule, MatInputModule, MatIconModule],
  template: `
    <app-modal
      [open]="open()"
      (closed)="closed.emit()"
      title="Sincronizza categorie"
      subtitle="Riassegna la categoria ai movimenti del periodo in base alle parole chiave"
      [submitAction]="submit"
      [busy]="busy()"
      submitLabel="Sincronizza"
    >
      <div class="range">
        <mat-form-field appearance="outline" class="field-block" subscriptSizing="dynamic">
          <mat-label>Dal</mat-label>
          <input matInput type="date" required [value]="range().start" (input)="patch('start', $event)" [disabled]="busy()" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="field-block" subscriptSizing="dynamic">
          <mat-label>Al</mat-label>
          <input matInput type="date" required [value]="range().end" (input)="patch('end', $event)" [disabled]="busy()" />
        </mat-form-field>
      </div>
      @if (range().start && range().end && range().start > range().end) {
        <div class="alert alert-warning">
          <mat-icon class="icon-sm">warning</mat-icon>
          <span>La data di inizio deve precedere la data di fine.</span>
        </div>
      }
      <p class="explain">
        Un movimento la cui descrizione contiene una parola chiave passa alla categoria corrispondente; se ne contiene di più
        categorie vince quella con priorità più bassa. I trasferimenti tra conti non vengono modificati e i movimenti senza
        corrispondenze mantengono la categoria attuale.
      </p>
    </app-modal>
  `,
  styles: `
    .range { display: flex; flex-direction: column; gap: 12px; }
    @media (min-width: 600px) { .range { flex-direction: row; } }
    .explain { font-size: var(--app-text-md); color: var(--app-text-secondary); }
  `,
})
export class CategorySyncModal {
  readonly open = input(false);
  readonly closed = output<void>();

  private readonly store = inject(CategoriesStore);
  protected readonly range = signal(createInitialRange());
  protected readonly busy = signal(false);
  private readonly invalidRange = computed(() => {
    const { start, end } = this.range();
    return !start || !end || start > end;
  });

  constructor() {
    effect(() => {
      if (this.open()) untracked(() => this.range.set(createInitialRange()));
    });
  }

  protected patch(key: "start" | "end", event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.range.update((r) => ({ ...r, [key]: value }));
  }

  protected readonly submit = async (): Promise<string> => {
    if (this.invalidRange()) throw new Error("Periodo non valido");
    this.busy.set(true);
    try {
      const { start, end } = this.range();
      // date yyyy-MM-dd senza orario: il backend considera solo il giorno, nessuno slittamento di fuso
      const result = await this.store.syncTransactions({ startDate: start, endDate: end });
      return result.updatedTransactions === 0
        ? `Nessun movimento da aggiornare (${result.examinedTransactions} analizzati)`
        : `${result.updatedTransactions} movimenti riclassificati su ${result.examinedTransactions} analizzati`;
    } finally {
      this.busy.set(false);
    }
  };
}
