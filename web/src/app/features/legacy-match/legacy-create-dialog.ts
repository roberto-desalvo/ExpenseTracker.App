import { ChangeDetectionStrategy, Component, computed, inject } from "@angular/core";
import { MatCheckbox } from "@angular/material/checkbox";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInput } from "@angular/material/input";
import { MatSelectModule } from "@angular/material/select";
import { MatTooltip } from "@angular/material/tooltip";
import { LegacyDbTransaction } from "../../core/models/LegacyMatch";
import { CategoriesStore } from "../../core/state/categories.store";
import { formatSignedCurrency } from "../../core/utils/format.utilities";
import { AppModal } from "../../shared/ui/app-modal";
import { LegacyMatchStore } from "./legacy-match.store";

const dayFormatter = new Intl.DateTimeFormat("it-IT", { day: "2-digit", month: "short", year: "numeric" });

/**
 * Finestra dei movimenti Excel su Contanti e Hype, che gli import automatici non recuperano: si creano
 * come nuovi movimenti, con testo e categoria modificabili. Un triangolo segnala i probabili duplicati
 * (stesso conto, mese, nome e somma di un movimento già a DB).
 */
@Component({
  selector: "app-legacy-create-dialog",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AppModal, MatCheckbox, MatFormFieldModule, MatInput, MatSelectModule, MatIconModule, MatTooltip],
  template: `
    <app-modal
      [open]="store.createDialogOpen()"
      [busy]="store.creating()"
      (closed)="store.createDialogOpen.set(false)"
      title="Movimenti di Contanti e Hype"
      subtitle="Non arrivano dagli import automatici: scegli quali creare, con testo e categoria."
      [submitLabel]="submitLabel()"
      cancelLabel="Chiudi"
      [maxWidth]="1040"
      [submitAction]="submit"
    >
      @if (store.createRows().length === 0) {
        <p class="empty">Nessun movimento di Contanti o Hype da creare in questo mese.</p>
      } @else {
        <div class="rows">
          @for (row of store.createRows(); track row.rowNumber) {
            @let draft = store.drafts().get(row.rowNumber)!;
            @let duplicate = store.duplicates().get(row.rowNumber);
            <div class="row" [class.excluded]="!draft.included">
              <mat-checkbox
                [checked]="draft.included"
                [disabled]="row.accountId === null"
                (change)="store.updateDraft(row.rowNumber, { included: $event.checked })"
                aria-label="Crea questo movimento"
              />
              <span class="warning">
                @if (duplicate) {
                  <mat-icon class="icon-sm" [matTooltip]="duplicateLabel(duplicate)" [attr.aria-label]="duplicateLabel(duplicate)" tabindex="0">warning</mat-icon>
                }
              </span>
              <div class="info">
                <span class="date">{{ day(row.date) }}</span>
                <span class="account" [class.missing]="row.accountId === null">
                  {{ row.accountName }}{{ row.accountId === null ? " · conto non trovato" : "" }}
                </span>
              </div>
              <span class="amount tabular" [class.income]="row.amount > 0">{{ formatSignedCurrency(row.amount) }}</span>
              <mat-form-field appearance="outline" subscriptSizing="dynamic" class="description">
                <mat-label>Descrizione</mat-label>
                <input matInput [value]="draft.description" maxlength="500" (input)="onDescription(row.rowNumber, $event)" />
              </mat-form-field>
              <mat-form-field appearance="outline" subscriptSizing="dynamic" class="category">
                <mat-label>Categoria</mat-label>
                <mat-select [value]="draft.categoryId" (selectionChange)="store.updateDraft(row.rowNumber, { categoryId: $event.value })">
                  <mat-option [value]="null">Senza categoria</mat-option>
                  @for (category of categories.all(); track category.id) {
                    <mat-option [value]="category.id">{{ category.name }}</mat-option>
                  }
                </mat-select>
                @if (row.excelCategory && draft.categoryId === null) {
                  <mat-hint>Excel: {{ row.excelCategory }}</mat-hint>
                }
              </mat-form-field>
            </div>
          }
        </div>
      }
    </app-modal>
  `,
  styles: `
    .empty { font-size: var(--app-text-md); color: var(--app-text-secondary); }
    .rows { display: flex; flex-direction: column; }
    .row {
      display: grid;
      grid-template-columns: auto 24px minmax(0, 1fr) auto;
      grid-template-areas:
        "check warn info amount"
        "description description description description"
        "category category category category";
      align-items: center;
      gap: 8px 10px;
      padding: 10px 0;
      border-bottom: 1px solid var(--app-border);
    }
    .row:last-child { border-bottom: none; }
    .row.excluded { opacity: 0.55; }
    mat-checkbox { grid-area: check; }
    .warning { grid-area: warn; display: grid; place-items: center; color: var(--app-expense); }
    .warning .mat-icon { cursor: help; }
    .info { grid-area: info; min-width: 0; display: flex; flex-direction: column; }
    .date { font-size: var(--app-text-md); font-weight: 600; color: var(--app-text); white-space: nowrap; }
    .account { font-size: var(--app-text-sm); color: var(--app-text-secondary); }
    .account.missing { color: var(--app-expense); }
    .amount { grid-area: amount; font-weight: 700; color: var(--app-text); white-space: nowrap; }
    .amount.income { color: var(--app-income); }
    .description { grid-area: description; width: 100%; }
    .category { grid-area: category; width: 100%; }
    @media (min-width: 900px) {
      .row {
        grid-template-columns: auto 24px 130px 100px minmax(0, 1fr) 220px;
        grid-template-areas: "check warn info amount description category";
      }
      .amount { text-align: right; }
    }
  `,
})
export class LegacyCreateDialog {
  protected readonly store = inject(LegacyMatchStore);
  protected readonly categories = inject(CategoriesStore);
  protected readonly formatSignedCurrency = formatSignedCurrency;

  protected readonly submitLabel = computed(() => {
    const count = this.store.draftsToCreate().length;
    return count === 1 ? "Crea 1 movimento" : `Crea ${count} movimenti`;
  });

  protected readonly submit = () => this.store.createDrafts();

  protected day(date: string | null) {
    return date ? dayFormatter.format(new Date(date)) : "—";
  }

  protected duplicateLabel(t: LegacyDbTransaction) {
    return `Esiste già un movimento uguale su ${t.account} nello stesso mese: «${t.description}» del ${this.day(t.date)}, ${formatSignedCurrency(t.amount)}`;
  }

  protected onDescription(rowNumber: number, event: Event) {
    this.store.updateDraft(rowNumber, { description: (event.target as HTMLInputElement).value });
  }
}
