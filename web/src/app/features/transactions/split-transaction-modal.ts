import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal, untracked } from "@angular/core";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatTooltip } from "@angular/material/tooltip";
import { NotificationService } from "../../core/api/notification.service";
import Transaction from "../../core/models/Transaction";
import { TransactionsStore } from "../../core/state/transactions.store";
import { formatCurrency, formatSignedCurrency } from "../../core/utils/format.utilities";
import { AppModal } from "../../shared/ui/app-modal";

/** Limite di voci (lo stesso del BE). */
const MAX_PARTS = 50;

const dayFormatter = new Intl.DateTimeFormat("it-IT", { day: "2-digit", month: "short", year: "numeric" });

interface SplitPart {
  key: number;
  /** importo in valore assoluto, come digitato */
  text: string;
}

/** "12.5" → 1250 centesimi; null se non è un importo positivo con al massimo 2 decimali. */
const toCents = (text: string): number | null => {
  const value = Number(text.trim().replace(",", "."));
  if (!text.trim() || !Number.isFinite(value) || value <= 0) return null;
  const cents = Math.round(value * 100);
  return Math.abs(value * 100 - cents) < 1e-6 ? cents : null;
};

const centsToText = (cents: number) => (cents / 100).toFixed(2);

/**
 * Scompone un movimento in più voci con gli stessi dati (descrizione, data, conto, categoria) e importi diversi,
 * la cui somma deve essere pari all'importo originale. La prima voce resta sul movimento originale.
 * Gli importi si inseriscono in valore assoluto: il segno è quello del movimento.
 */
@Component({
  selector: "app-split-transaction-modal",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AppModal, MatButton, MatIconButton, MatFormFieldModule, MatIconModule, MatInputModule, MatTooltip],
  template: `
    <app-modal
      [open]="transaction() !== null"
      (closed)="closed.emit()"
      title="Scomponi movimento"
      subtitle="Le voci avranno gli stessi dati del movimento; la somma degli importi deve corrispondere all'originale."
      [submitAction]="submit"
      submitLabel="Scomponi"
      [submitDisabled]="!canSubmit()"
      [maxWidth]="520"
    >
      @if (transaction(); as source) {
        <div class="item source">
          <span class="date">{{ day(source.date) }}</span>
          <span class="text">
            <span class="description">{{ source.description || "—" }}</span>
            <span class="account">{{ source.account }}{{ source.category ? " · " + source.category : "" }}</span>
          </span>
          <span class="amount tabular" [class.income]="source.amount > 0">{{ formatSignedCurrency(source.amount) }}</span>
        </div>

        <div class="parts">
          @for (part of parts(); track part.key; let i = $index) {
            <div class="part">
              <mat-form-field appearance="outline" subscriptSizing="dynamic" class="part-field">
                <mat-label>Voce {{ i + 1 }}{{ i === 0 ? " (originale)" : "" }}</mat-label>
                <span matTextPrefix>{{ source.amount < 0 ? "−" : "+" }}&nbsp;€&nbsp;</span>
                <input
                  matInput
                  type="number"
                  required
                  min="0.01"
                  step="0.01"
                  inputmode="decimal"
                  [value]="part.text"
                  (input)="setPart(part.key, $any($event.target).value)"
                />
              </mat-form-field>
              <button
                matIconButton
                type="button"
                aria-label="Rimuovi voce"
                matTooltip="Rimuovi voce"
                [disabled]="parts().length <= 2"
                (click)="removePart(part.key)"
              >
                <mat-icon class="icon-sm">close</mat-icon>
              </button>
            </div>
          }
        </div>

        <div class="tools">
          <button matButton type="button" [disabled]="parts().length >= MAX_PARTS" (click)="addPart()">
            <mat-icon>add</mat-icon> Aggiungi voce
          </button>
          <button matButton type="button" (click)="splitEvenly()">
            <mat-icon>balance</mat-icon> Parti uguali
          </button>
        </div>

        <div class="summary" [class.ok]="remainingCents() === 0" [class.over]="remainingCents() < 0" role="status">
          <span>Totale voci <strong class="tabular">{{ formatCurrency(sumCents() / 100) }}</strong> su {{ formatCurrency(totalCents() / 100) }}</span>
          <span class="tabular">
            @if (remainingCents() > 0) {
              Da ripartire {{ formatCurrency(remainingCents() / 100) }}
            } @else if (remainingCents() < 0) {
              In eccesso {{ formatCurrency(-remainingCents() / 100) }}
            } @else {
              <mat-icon class="icon-sm">check_circle</mat-icon> Quadra
            }
          </span>
        </div>
      }
    </app-modal>
  `,
  styles: `
    .item { display: flex; align-items: center; gap: 12px; width: 100%; min-width: 0; }
    .source {
      padding: 8px 12px; border: 1px solid var(--app-border); border-radius: var(--app-radius-control);
      background-color: var(--app-surface-alt); box-sizing: border-box;
    }
    .date { font-size: var(--app-text-sm); font-weight: 600; color: var(--app-text); white-space: nowrap; }
    .text { display: flex; flex-direction: column; flex: 1; min-width: 0; }
    .description {
      font-size: var(--app-text-md); font-weight: 500; color: var(--app-text);
      overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
    }
    .account { font-size: var(--app-text-sm); color: var(--app-text-secondary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .amount { font-size: var(--app-text-md); font-weight: 700; color: var(--app-text); white-space: nowrap; }
    .amount.income { color: var(--app-income); }
    .parts { display: flex; flex-direction: column; gap: 12px; }
    .part { display: flex; align-items: center; gap: 4px; }
    .part-field { flex: 1; min-width: 0; }
    .tools { display: flex; flex-wrap: wrap; gap: 8px; margin-top: -4px; }
    .summary {
      display: flex; flex-wrap: wrap; justify-content: space-between; gap: 4px 12px; align-items: center;
      padding: 8px 12px; border-radius: var(--app-radius-control);
      background-color: var(--app-surface-alt); color: var(--app-text-secondary); font-size: var(--app-text-sm);
    }
    .summary > span:last-child { display: inline-flex; align-items: center; gap: 4px; font-weight: 600; color: var(--app-text); }
    .summary.ok > span:last-child { color: var(--app-income); }
    .summary.over > span:last-child { color: var(--app-expense); }
  `,
})
export class SplitTransactionModal {
  /** Movimento da scomporre; null = modale chiusa. */
  readonly transaction = input<Transaction | null>(null);
  readonly closed = output<void>();

  private readonly store = inject(TransactionsStore);
  private readonly notifications = inject(NotificationService);

  protected readonly formatCurrency = formatCurrency;
  protected readonly formatSignedCurrency = formatSignedCurrency;
  protected readonly MAX_PARTS = MAX_PARTS;

  protected readonly parts = signal<SplitPart[]>([]);
  private nextKey = 0;

  protected readonly totalCents = computed(() => Math.round(Math.abs(this.transaction()?.amount ?? 0) * 100));
  private readonly partCents = computed(() => this.parts().map((p) => toCents(p.text)));
  protected readonly sumCents = computed(() => this.partCents().reduce<number>((sum, c) => sum + (c ?? 0), 0));
  protected readonly remainingCents = computed(() => this.totalCents() - this.sumCents());
  protected readonly canSubmit = computed(
    () => this.parts().length >= 2 && this.partCents().every((c) => c !== null) && this.remainingCents() === 0,
  );

  constructor() {
    // a ogni apertura si parte da due voci a metà importo
    effect(() => {
      const source = this.transaction();
      untracked(() => {
        this.parts.set([]);
        if (source) this.splitEvenly(2);
      });
    });
  }

  protected day(date: Date | string) {
    return dayFormatter.format(new Date(date));
  }

  protected setPart(key: number, text: string) {
    this.parts.update((parts) => parts.map((p) => (p.key === key ? { ...p, text } : p)));
  }

  /** Nuova voce precompilata con l'importo ancora da ripartire (se c'è). */
  protected addPart() {
    const remaining = this.remainingCents();
    this.parts.update((parts) => [...parts, { key: this.nextKey++, text: remaining > 0 ? centsToText(remaining) : "" }]);
  }

  protected removePart(key: number) {
    this.parts.update((parts) => (parts.length > 2 ? parts.filter((p) => p.key !== key) : parts));
  }

  /** Ripartisce l'importo in parti uguali tra le voci; i centesimi di resto vanno alle prime voci. */
  protected splitEvenly(count = this.parts().length) {
    const total = this.totalCents();
    const n = Math.min(Math.max(count, 2), MAX_PARTS, Math.max(total, 2));
    const base = Math.floor(total / n);
    const extra = total - base * n;
    this.parts.set(
      Array.from({ length: n }, (_, i) => ({ key: this.nextKey++, text: centsToText(base + (i < extra ? 1 : 0)) })),
    );
  }

  protected readonly submit = async (): Promise<string> => {
    const source = this.transaction();
    const cents = this.partCents();
    if (!source || !this.canSubmit() || cents.some((c) => c === null)) {
      this.notifications.showError("La somma delle voci deve corrispondere all'importo originale");
      throw new Error("invalid split");
    }
    const sign = source.amount < 0 ? -1 : 1;
    await this.store.splitTransaction(
      source.id,
      cents.map((c) => (sign * (c as number)) / 100),
    );
    return `Movimento scomposto in ${cents.length} voci`;
  };
}
