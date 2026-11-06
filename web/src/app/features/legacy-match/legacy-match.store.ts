import { Injectable, computed, inject, signal } from "@angular/core";
import { LegacyMatchApi } from "../../core/api/legacy-match.api";
import { NotificationService } from "../../core/api/notification.service";
import { LegacyDbTransaction, LegacyExcelRow, LegacyMonth, LegacySheet } from "../../core/models/LegacyMatch";
import { TransactionsStore } from "../../core/state/transactions.store";

/** Coppia riga Excel ↔ movimento a DB, con i valori (modificabili) che verranno salvati sul movimento. */
export interface LegacyPair {
  rowNumber: number;
  transactionId: number;
  description: string;
  categoryId: number | null;
  included: boolean;
  suggested: boolean;
}

/** Descrizione proposta per il movimento abbinato: "descrizione DB - descrizione Excel". */
export const defaultPairDescription = (row: LegacyExcelRow, transaction: LegacyDbTransaction) =>
  [transaction.description.trim(), row.description.trim()].filter((part) => part.length > 0).join(" - ");

/**
 * Stato della pagina di abbinamento dello storico Excel: file scelto, foglio (mese) corrente, righe e
 * movimenti del mese e coppie, inizializzate dai suggerimenti del BE e modificabili prima del salvataggio.
 * Fornito dalla pagina (non root): si azzera quando si esce.
 */
@Injectable()
export class LegacyMatchStore {
  private readonly api = inject(LegacyMatchApi);
  private readonly notifications = inject(NotificationService);
  private readonly transactions = inject(TransactionsStore);

  readonly file = signal<File | null>(null);
  readonly sheets = signal<LegacySheet[]>([]);
  readonly selectedSheet = signal<string | null>(null);
  readonly month = signal<LegacyMonth | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly selectedRow = signal<number | null>(null);
  readonly pairs = signal<ReadonlyMap<number, LegacyPair>>(new Map());

  readonly rows = computed(() => this.month()?.excelRows ?? []);
  readonly dbTransactions = computed(() => this.month()?.dbTransactions ?? []);

  /** Coppie in ordine di riga Excel. */
  readonly pairList = computed(() => [...this.pairs().values()].sort((a, b) => a.rowNumber - b.rowNumber));

  /** Numero progressivo della coppia (badge mostrato su entrambi i lati). */
  readonly pairIndexByRow = computed(() => new Map(this.pairList().map((p, i) => [p.rowNumber, i + 1])));

  readonly rowByTransaction = computed(() => new Map(this.pairList().map((p) => [p.transactionId, p.rowNumber])));

  readonly toSave = computed(() => this.pairList().filter((p) => p.included));

  private readonly rowsByNumber = computed(() => new Map(this.rows().map((r) => [r.rowNumber, r])));
  private readonly transactionsById = computed(() => new Map(this.dbTransactions().map((t) => [t.id, t])));

  row(rowNumber: number) {
    return this.rowsByNumber().get(rowNumber);
  }

  transaction(id: number) {
    return this.transactionsById().get(id);
  }

  /** Nuovo file: legge i fogli mensili e apre il primo. */
  async openFile(file: File) {
    this.loading.set(true);
    try {
      const sheets = await this.api.getSheets(file);
      this.file.set(file);
      this.sheets.set(sheets);
      this.month.set(null);
      this.pairs.set(new Map());
      this.selectedSheet.set(null);
    } catch {
      return; // errore notificato dall'interceptor
    } finally {
      this.loading.set(false);
    }

    const first = this.sheets()[0];
    if (first) await this.loadSheet(first.name);
  }

  async loadSheet(name: string) {
    const file = this.file();
    if (!file) return;

    this.selectedSheet.set(name);
    this.selectedRow.set(null);
    this.loading.set(true);
    try {
      const month = await this.api.getMonth(file, name);
      // un caricamento più recente (cambio rapido di mese) vince
      if (this.selectedSheet() !== name) return;
      this.month.set(month);
      this.pairs.set(this.initialPairs(month));
    } catch {
      // errore notificato dall'interceptor
    } finally {
      if (this.selectedSheet() === name) this.loading.set(false);
    }
  }

  /** Click su una riga Excel: la seleziona (o deseleziona) per l'abbinamento manuale. */
  selectRow(rowNumber: number) {
    this.selectedRow.update((current) => (current === rowNumber ? null : rowNumber));
  }

  /**
   * Click su un movimento: con una riga Excel selezionata crea la coppia (sostituendo quelle esistenti
   * della riga e del movimento); senza selezione, se il movimento è già abbinato ne seleziona la riga.
   */
  selectTransaction(transactionId: number) {
    const rowNumber = this.selectedRow();
    if (rowNumber === null) {
      const pairedRow = this.rowByTransaction().get(transactionId);
      if (pairedRow !== undefined) this.selectedRow.set(pairedRow);
      return;
    }

    const row = this.row(rowNumber);
    const transaction = this.transaction(transactionId);
    if (!row || !transaction) return;

    this.pairs.update((current) => {
      const next = new Map([...current].filter(([, p]) => p.transactionId !== transactionId));
      next.set(rowNumber, {
        rowNumber,
        transactionId,
        description: defaultPairDescription(row, transaction),
        categoryId: row.suggestedCategoryId,
        included: true,
        suggested: false,
      });
      return next;
    });
    this.selectedRow.set(null);
  }

  unpair(rowNumber: number) {
    this.pairs.update((current) => {
      const next = new Map(current);
      next.delete(rowNumber);
      return next;
    });
  }

  updatePair(rowNumber: number, patch: Partial<Pick<LegacyPair, "description" | "categoryId" | "included">>) {
    this.pairs.update((current) => {
      const pair = current.get(rowNumber);
      if (!pair) return current;
      const next = new Map(current);
      next.set(rowNumber, { ...pair, ...patch });
      return next;
    });
  }

  /** Salva le coppie incluse e ricarica il mese (i movimenti salvati risultano già abbinati). */
  async save() {
    const items = this.toSave().map((p) => ({
      transactionId: p.transactionId,
      description: p.description.trim(),
      categoryId: p.categoryId,
    }));
    if (items.length === 0) return;
    if (items.some((i) => i.description.length === 0)) {
      this.notifications.showError("Ogni abbinamento deve avere una descrizione");
      return;
    }

    this.saving.set(true);
    try {
      const saved = await this.api.apply(items);
      this.notifications.showSuccess(`${saved} ${saved === 1 ? "movimento aggiornato" : "movimenti aggiornati"}`);
      this.transactions.notifyExternalChange();
    } catch {
      return; // errore notificato dall'interceptor
    } finally {
      this.saving.set(false);
    }

    const sheet = this.selectedSheet();
    if (sheet) await this.loadSheet(sheet);
  }

  private initialPairs(month: LegacyMonth) {
    const rows = new Map(month.excelRows.map((r) => [r.rowNumber, r]));
    const transactions = new Map(month.dbTransactions.map((t) => [t.id, t]));
    const pairs = new Map<number, LegacyPair>();
    for (const suggestion of month.suggestions) {
      const row = rows.get(suggestion.rowNumber);
      const transaction = transactions.get(suggestion.transactionId);
      if (!row || !transaction) continue;
      pairs.set(row.rowNumber, {
        rowNumber: row.rowNumber,
        transactionId: transaction.id,
        description: defaultPairDescription(row, transaction),
        categoryId: row.suggestedCategoryId,
        included: true,
        suggested: true,
      });
    }
    return pairs;
  }
}
