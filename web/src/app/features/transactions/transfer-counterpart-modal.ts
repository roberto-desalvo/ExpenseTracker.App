import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal, untracked } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";
import { MatRadioModule } from "@angular/material/radio";
import { TransactionApi } from "../../core/api/transaction.api";
import { NotificationService } from "../../core/api/notification.service";
import Transaction from "../../core/models/Transaction";
import { TransactionsStore } from "../../core/state/transactions.store";
import { formatCurrency, formatSignedCurrency } from "../../core/utils/format.utilities";
import { AppModal } from "../../shared/ui/app-modal";

const dayFormatter = new Intl.DateTimeFormat("it-IT", { day: "2-digit", month: "short", year: "numeric" });

/**
 * Completa un trasferimento a cui manca un ramo (o crea quello di un movimento Money transfers che non ne ha): elenca i movimenti con importo opposto, data nello stesso mese
 * e conto diverso (dal BE) e collega quello scelto, che prende la data del movimento di partenza.
 */
@Component({
  selector: "app-transfer-counterpart-modal",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AppModal, MatIconModule, MatRadioModule],
  template: `
    <app-modal
      [open]="transaction() !== null"
      (closed)="closed.emit()"
      title="Collega il movimento corrispondente"
      [subtitle]="subtitle()"
      [submitAction]="submit"
      submitLabel="Collega"
      [submitDisabled]="selected() === null"
      [maxWidth]="560"
    >
      @if (transaction(); as source) {
        <div class="source">
          <span class="label">Movimento di partenza</span>
          <div class="item">
            <span class="date">{{ day(source.date) }}</span>
            <span class="text">
              <span class="description">{{ source.description || "—" }}</span>
              <span class="account">{{ source.account }}</span>
            </span>
            <span class="amount tabular" [class.income]="source.amount > 0">{{ formatSignedCurrency(source.amount) }}</span>
          </div>
        </div>

        @if (loading()) {
          <p class="muted">Ricerca dei movimenti corrispondenti...</p>
        } @else if (candidates().length === 0) {
          <div class="alert alert-info" role="status">
            <mat-icon class="icon-sm">info</mat-icon>
            <span>
              Nessun movimento di {{ formatSignedCurrency(-source.amount) }} nello stesso mese su un altro conto,
              libero da altri trasferimenti.
            </span>
          </div>
        } @else {
          <mat-radio-group
            class="candidates"
            aria-label="Movimento corrispondente"
            [value]="selectedId()"
            (change)="selectedId.set($event.value)"
          >
            @for (c of candidates(); track c.id) {
              <mat-radio-button class="candidate" [value]="c.id">
                <span class="item">
                  <span class="date">{{ day(c.date) }}</span>
                  <span class="text">
                    <span class="description">{{ c.description || "—" }}</span>
                    <span class="account">{{ c.account }}</span>
                  </span>
                  <span class="amount tabular" [class.income]="c.amount > 0">{{ formatSignedCurrency(c.amount) }}</span>
                </span>
              </mat-radio-button>
            }
          </mat-radio-group>
          <p class="muted">La data del movimento scelto diventerà {{ day(source.date) }}.</p>
        }
      }
    </app-modal>
  `,
  styles: `
    .source { display: flex; flex-direction: column; gap: 6px; }
    .label { font-size: var(--app-text-sm); font-weight: 600; color: var(--app-text-muted); }
    .source > .item {
      padding: 8px 12px; border: 1px solid var(--app-border); border-radius: var(--app-radius-control);
      background-color: var(--app-surface-alt);
    }
    .item { display: flex; align-items: center; gap: 12px; width: 100%; min-width: 0; }
    .date { font-size: var(--app-text-sm); font-weight: 600; color: var(--app-text); white-space: nowrap; }
    .text { display: flex; flex-direction: column; flex: 1; min-width: 0; }
    .description {
      font-size: var(--app-text-md); font-weight: 500; color: var(--app-text);
      overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
    }
    .account { font-size: var(--app-text-sm); color: var(--app-text-secondary); }
    .amount { font-size: var(--app-text-md); font-weight: 700; color: var(--app-text); white-space: nowrap; }
    .amount.income { color: var(--app-income); }
    .candidates { display: flex; flex-direction: column; gap: 4px; }
    .candidate {
      display: block; border: 1px solid var(--app-border); border-radius: var(--app-radius-control); padding-right: 12px;
    }
    .candidate ::ng-deep .mdc-form-field { width: 100%; }
    .candidate ::ng-deep label { flex: 1; min-width: 0; cursor: pointer; }
    .muted { font-size: var(--app-text-sm); color: var(--app-text-muted); }
  `,
})
export class TransferCounterpartModal {
  /** Movimento di partenza (con trasferimento incompleto); null = modale chiusa. */
  readonly transaction = input<Transaction | null>(null);
  readonly closed = output<void>();

  private readonly api = inject(TransactionApi);
  private readonly store = inject(TransactionsStore);
  private readonly notifications = inject(NotificationService);

  protected readonly candidates = signal<Transaction[]>([]);
  protected readonly loading = signal(false);
  protected readonly selectedId = signal<number | null>(null);
  protected readonly selected = computed(() => this.candidates().find((c) => c.id === this.selectedId()) ?? null);
  protected readonly formatSignedCurrency = formatSignedCurrency;
  protected readonly subtitle = computed(() =>
    this.transaction()?.transferMissing
      ? "Il movimento è in Money transfers ma non ha un trasferimento: scegli il corrispondente, verrà creato il trasferimento tra i due."
      : "Il trasferimento ha un solo ramo: scegli il movimento che lo completa.",
  );

  private loadId = 0;

  constructor() {
    // a ogni apertura si ricaricano i candidati; nessuno è preselezionato, "Collega" si abilita dopo la scelta
    effect(() => {
      const source = this.transaction();
      untracked(() => void this.load(source));
    });
  }

  protected day(date: Date | string) {
    return dayFormatter.format(new Date(date));
  }

  private async load(source: Transaction | null) {
    const id = ++this.loadId;
    this.candidates.set([]);
    this.selectedId.set(null);
    if (!source) {
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    try {
      const candidates = await this.api.getTransferCandidates(source.id);
      if (id !== this.loadId) return;
      this.candidates.set(candidates);
    } catch {
      // l'errore è già notificato dal layer API
    } finally {
      if (id === this.loadId) this.loading.set(false);
    }
  }

  protected readonly submit = async (): Promise<string> => {
    const source = this.transaction();
    const counterpart = this.selected();
    if (!source || !counterpart) {
      this.notifications.showError("Seleziona il movimento corrispondente");
      throw new Error("no counterpart selected");
    }
    await this.store.linkTransferCounterpart(source.id, counterpart.id);
    return `Trasferimento completato con ${counterpart.account} (${formatCurrency(Math.abs(counterpart.amount))})`;
  };
}
