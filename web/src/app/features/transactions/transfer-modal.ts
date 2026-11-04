import { ChangeDetectionStrategy, Component, computed, inject, signal } from "@angular/core";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { TransferPayload } from "../../core/models/Transfer";
import { AccountsStore } from "../../core/state/accounts.store";
import { QuickActionsService } from "../../core/state/quick-actions.service";
import { TransactionsStore } from "../../core/state/transactions.store";
import { AppModal } from "../../shared/ui/app-modal";
import { EntityAutocomplete } from "../../shared/ui/entity-autocomplete";

const createDefaultTransfer = (): TransferPayload => ({
  fromAccountId: 0,
  toAccountId: 0,
  amount: 0,
  description: "",
  date: new Date(),
});

/** Nuovo trasferimento tra due conti (non conta come entrata né come uscita). */
@Component({
  selector: "app-transfer-modal",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AppModal, EntityAutocomplete, MatDatepickerModule, MatFormFieldModule, MatInputModule],
  template: `
    <app-modal
      [open]="quickActions.transferModalOpen()"
      (closed)="close()"
      title="Nuovo trasferimento"
      subtitle="Sposta denaro tra due conti: non conta come entrata né come uscita"
      [submitAction]="submit"
      submitLabel="Salva"
    >
      <mat-form-field appearance="outline" class="field-block">
        <mat-label>Data</mat-label>
        <input matInput [matDatepicker]="picker" [value]="transfer().date ?? null" (dateChange)="patch({ date: $event.value })" />
        <mat-datepicker-toggle matIconSuffix [for]="picker" />
        <mat-datepicker #picker />
      </mat-form-field>

      <mat-form-field appearance="outline" class="field-block">
        <mat-label>Descrizione</mat-label>
        <input matInput [value]="transfer().description" (input)="patch({ description: inputValue($event) })" />
      </mat-form-field>

      <mat-form-field appearance="outline" class="field-block">
        <mat-label>Importo</mat-label>
        <input matInput type="number" [value]="transfer().amount" (input)="patch({ amount: toNumber($event) })" />
      </mat-form-field>

      <app-entity-autocomplete
        label="Da conto"
        [options]="accountOptions()"
        [value]="transfer().fromAccountId"
        (valueChange)="patch({ fromAccountId: $event ?? 0 })"
      />
      <app-entity-autocomplete
        label="A conto"
        [options]="accountOptions()"
        [value]="transfer().toAccountId"
        (valueChange)="patch({ toAccountId: $event ?? 0 })"
      />
    </app-modal>
  `,
})
export class TransferModal {
  protected readonly quickActions = inject(QuickActionsService);
  private readonly transactions = inject(TransactionsStore);
  private readonly accounts = inject(AccountsStore);

  protected readonly transfer = signal<TransferPayload>(createDefaultTransfer());
  protected readonly accountOptions = computed(() => this.accounts.all().map((a) => ({ id: a.id, label: a.name || "" })));

  protected inputValue(event: Event) {
    return (event.target as HTMLInputElement).value;
  }

  protected toNumber(event: Event) {
    return Number(this.inputValue(event));
  }

  protected patch(changes: Partial<TransferPayload>) {
    this.transfer.update((prev) => ({ ...prev, ...changes }));
  }

  protected close() {
    this.quickActions.closeTransferModal();
    this.transfer.set(createDefaultTransfer());
  }

  protected readonly submit = async (): Promise<string> => {
    const transfer = this.transfer();
    await this.transactions.addTransfer({
      ...transfer,
      amount: Number(transfer.amount),
      description: transfer.description.trim(),
    });
    return "Trasferimento creato";
  };
}
