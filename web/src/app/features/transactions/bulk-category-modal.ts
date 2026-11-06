import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from "@angular/core";
import { CategoriesStore } from "../../core/state/categories.store";
import { TransactionsStore } from "../../core/state/transactions.store";
import { AppModal } from "../../shared/ui/app-modal";
import { EntityAutocomplete } from "../../shared/ui/entity-autocomplete";

/** Assegna la stessa categoria a più movimenti selezionati (vuota = nessuna categoria). */
@Component({
  selector: "app-bulk-category-modal",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AppModal, EntityAutocomplete],
  template: `
    <app-modal
      [open]="open()"
      (closed)="closed.emit()"
      title="Assegna categoria"
      [subtitle]="subtitle()"
      [submitAction]="submit"
      submitLabel="Assegna"
    >
      <app-entity-autocomplete
        label="Categoria"
        [options]="categoryOptions()"
        [value]="categoryId()"
        (valueChange)="categoryId.set($event)"
      />
    </app-modal>
  `,
})
export class BulkCategoryModal {
  readonly open = input(false);
  readonly ids = input.required<number[]>();
  readonly closed = output<void>();
  /** emesso dopo l'assegnazione riuscita */
  readonly assigned = output<void>();

  private readonly categories = inject(CategoriesStore);
  private readonly transactions = inject(TransactionsStore);

  protected readonly categoryId = signal<number | null>(null);
  protected readonly categoryOptions = computed(() =>
    this.categories.all().map((c) => ({ id: c.id, label: c.name || c.description || "" })),
  );
  protected readonly subtitle = computed(() => {
    const count = this.ids().length;
    return `La categoria verrà applicata a ${count === 1 ? "1 movimento" : `${count} movimenti`}. Lasciala vuota per rimuoverla.`;
  });

  constructor() {
    // a ogni apertura si parte senza categoria scelta
    effect(() => {
      if (this.open()) this.categoryId.set(null);
    });
  }

  protected readonly submit = async (): Promise<string> => {
    const ids = this.ids();
    await this.transactions.assignCategory(ids, this.categoryId());
    this.assigned.emit();
    return ids.length === 1 ? "Categoria assegnata a 1 movimento" : `Categoria assegnata a ${ids.length} movimenti`;
  };
}
