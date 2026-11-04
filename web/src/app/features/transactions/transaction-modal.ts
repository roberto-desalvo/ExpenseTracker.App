import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from "@angular/core";
import { MatButtonToggleModule } from "@angular/material/button-toggle";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { AccountsStore } from "../../core/state/accounts.store";
import { CategoriesStore } from "../../core/state/categories.store";
import { QuickActionsService } from "../../core/state/quick-actions.service";
import { TransactionsStore } from "../../core/state/transactions.store";
import Transaction from "../../core/models/Transaction";
import { AppModal } from "../../shared/ui/app-modal";
import { EntityAutocomplete } from "../../shared/ui/entity-autocomplete";

type Kind = "expense" | "income";

/** Creazione/modifica di una transazione (entrata o uscita su un conto). */
@Component({
  selector: "app-transaction-modal",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AppModal,
    EntityAutocomplete,
    MatButtonToggleModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
  ],
  template: `
    <app-modal
      [open]="quickActions.transactionModalOpen()"
      (closed)="quickActions.closeTransactionModal()"
      [title]="isEdit() ? 'Modifica transazione' : 'Nuova transazione'"
      [subtitle]="isEdit() ? undefined : createSubtitle"
      [submitAction]="submit"
      [submitLabel]="isEdit() ? 'Salva modifiche' : 'Aggiungi'"
    >
      <mat-button-toggle-group
        class="segmented kind"
        [value]="kind()"
        (change)="changeKind($event.value)"
        aria-label="Tipo di movimento"
        hideSingleSelectionIndicator
      >
        <mat-button-toggle value="expense" class="expense">
          <mat-icon class="icon-sm">north_east</mat-icon> Uscita
        </mat-button-toggle>
        <mat-button-toggle value="income" class="income">
          <mat-icon class="icon-sm">south_west</mat-icon> Entrata
        </mat-button-toggle>
      </mat-button-toggle-group>

      <mat-form-field appearance="outline" class="field-block">
        <mat-label>Importo</mat-label>
        <span matTextPrefix>€&nbsp;</span>
        <input
          matInput
          type="number"
          required
          min="0"
          step="0.01"
          inputmode="decimal"
          [value]="amountText()"
          (input)="changeAmount($event)"
        />
      </mat-form-field>

      <mat-form-field appearance="outline" class="field-block">
        <mat-label>Descrizione</mat-label>
        <input
          matInput
          placeholder="Es. Spesa supermercato"
          [value]="current()?.description ?? ''"
          (input)="patch({ description: inputValue($event) })"
        />
      </mat-form-field>

      <mat-form-field appearance="outline" class="field-block">
        <mat-label>Data</mat-label>
        <input matInput [matDatepicker]="picker" [value]="current()?.date ?? null" (dateChange)="changeDate($event.value)" />
        <mat-datepicker-toggle matIconSuffix [for]="picker" />
        <mat-datepicker #picker />
      </mat-form-field>

      <app-entity-autocomplete
        label="Categoria"
        [options]="categoryOptions()"
        [value]="current()?.categoryId"
        (valueChange)="changeCategory($event)"
      />
      <app-entity-autocomplete
        label="Conto"
        required
        [options]="accountOptions()"
        [value]="current()?.accountId"
        (valueChange)="changeAccount($event)"
      />
    </app-modal>
  `,
  styles: `
    .kind { width: 100%; }
    .kind .mat-button-toggle { flex: 1; }
    .kind .mat-icon { vertical-align: -4px; margin-right: 4px; }
    .kind .mat-button-toggle-checked.expense { color: var(--app-expense); }
    .kind .mat-button-toggle-checked.income { color: var(--app-income); }
  `,
})
export class TransactionModal {
  protected readonly quickActions = inject(QuickActionsService);
  private readonly transactions = inject(TransactionsStore);
  private readonly accounts = inject(AccountsStore);
  private readonly categories = inject(CategoriesStore);

  protected readonly createSubtitle = "Registra un'entrata o un'uscita su uno dei tuoi conti";
  protected readonly current = this.quickActions.currentTransaction;
  protected readonly kind = signal<Kind>("expense");
  protected readonly amountText = signal("");
  protected readonly isEdit = computed(() => (this.current()?.id ?? 0) > 0);

  protected readonly categoryOptions = computed(() =>
    this.categories.all().map((c) => ({ id: c.id, label: c.name || c.description || "" })),
  );
  protected readonly accountOptions = computed(() => this.accounts.all().map((a) => ({ id: a.id, label: a.name || "" })));

  constructor() {
    // all'apertura il tipo deriva dal segno dell'importo esistente
    effect(() => {
      if (!this.quickActions.transactionModalOpen()) return;
      const amount = untracked(this.current)?.amount ?? 0;
      untracked(() => {
        this.kind.set(amount > 0 ? "income" : "expense");
        this.amountText.set(amount ? String(Math.abs(amount)) : "");
      });
    });
  }

  protected inputValue(event: Event) {
    return (event.target as HTMLInputElement).value;
  }

  protected patch(changes: Partial<Transaction>) {
    this.current.update((prev) => {
      const base = prev ?? new Transaction(0, new Date(), "", 0, 0, "", 0, "");
      return Object.assign(
        new Transaction(base.id, base.date, base.description, base.amount, base.categoryId, base.category, base.accountId, base.account),
        changes,
      );
    });
  }

  private signedAmount(value: number, kind: Kind) {
    return kind === "expense" ? -Math.abs(value) : Math.abs(value);
  }

  protected changeKind(kind: Kind | null) {
    if (!kind) return;
    this.kind.set(kind);
    const amount = this.current()?.amount;
    if (amount) this.patch({ amount: this.signedAmount(amount, kind) });
  }

  protected changeAmount(event: Event) {
    const text = this.inputValue(event);
    this.amountText.set(text);
    this.patch({ amount: this.signedAmount(Number(text) || 0, this.kind()) });
  }

  protected changeDate(date: Date | null) {
    this.patch({ date: date ?? new Date() });
  }

  protected changeCategory(id: number | null) {
    const category = this.categories.all().find((c) => c.id === id);
    if (!category) return;
    this.patch({ categoryId: category.id, category: category.description });
  }

  protected changeAccount(id: number | null) {
    const account = this.accounts.all().find((a) => a.id === id);
    if (!account) return;
    this.patch({ accountId: account.id, account: account.name });
  }

  protected readonly submit = async (): Promise<string> => {
    const transaction = this.current();
    if (!transaction) throw new Error("Nessuna transazione");
    const isEdit = transaction.id > 0;
    if (isEdit) {
      await this.transactions.updateTransaction(transaction);
    } else {
      await this.transactions.addTransaction(transaction);
    }
    return isEdit ? "Transazione modificata" : "Transazione creata";
  };
}
