import { Injectable, computed, inject, signal } from "@angular/core";
import { LegacyMatchApi } from "../../core/api/legacy-match.api";
import { NotificationService } from "../../core/api/notification.service";
import { LegacyCreateItem, LegacyDbTransaction, LegacyExcelRow, LegacyMonth, LegacySheet } from "../../core/models/LegacyMatch";
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

/** Riga di Contanti/Hype da creare come nuovo movimento, con i valori modificabili. */
export interface LegacyDraft {
  rowNumber: number;
  description: string;
  categoryId: number | null;
  included: boolean;
}

const normalizeText = (value: string) => value.trim().replace(/\s+/g, " ").toLocaleLowerCase("it-IT");

/**
 * Movimento già a DB che sembra lo stesso della riga da creare: stesso conto, stesso mese (i movimenti
 * caricati sono già quelli del mese del foglio), stessa somma e stesso nome (quello modificato o quello Excel).
 */
export const findDuplicate = (
  row: LegacyExcelRow,
  description: string,
  transactions: readonly LegacyDbTransaction[],
): LegacyDbTransaction | undefined => {
  if (row.accountId === null) return undefined;
  const names = new Set([normalizeText(description), normalizeText(row.description)]);
  return transactions.find(
    (t) =>
      t.accountId === row.accountId &&
      Math.round(t.amount * 100) === Math.round(row.amount * 100) &&
      names.has(normalizeText(t.description)),
  );
};

/** Righe del foglio già gestite nella sessione: salvate oppure segnate a mano come abbinate. */
interface HandledRows {
  saved: number[];
  dismissed: number[];
}

const storageKey = (fileName: string, sheet: string) => `legacy-match:${fileName}:${sheet}`;

// sessionStorage può mancare o lanciare (navigazione privata, storage bloccato): si resta in memoria
const readHandled = (key: string): HandledRows => {
  try {
    const parsed = JSON.parse(sessionStorage.getItem(key) ?? "null") as HandledRows | null;
    return { saved: parsed?.saved ?? [], dismissed: parsed?.dismissed ?? [] };
  } catch {
    return { saved: [], dismissed: [] };
  }
};

const writeHandled = (key: string, value: HandledRows) => {
  try {
    sessionStorage.setItem(key, JSON.stringify(value));
  } catch {
    // ignorato: lo stato resta valido finché la pagina è aperta
  }
};

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
  /** Righe salvate in questa sessione (anche nei caricamenti successivi dello stesso foglio). */
  readonly savedRows = signal<ReadonlySet<number>>(new Set());
  /** Righe segnate a mano come abbinate, senza un movimento. */
  readonly dismissedRows = signal<ReadonlySet<number>>(new Set());

  readonly rows = computed(() => this.month()?.excelRows ?? []);
  /** Righe da abbinare a movimenti esistenti (escluse Contanti e Hype, che vanno create). */
  readonly matchRows = computed(() => this.rows().filter((r) => !r.toCreate));

  /** Righe di Contanti/Hype ancora da creare, con i valori modificabili. */
  readonly drafts = signal<ReadonlyMap<number, LegacyDraft>>(new Map());
  readonly createDialogOpen = signal(false);
  readonly creating = signal(false);
  readonly createRows = computed(() => this.rows().filter((r) => r.toCreate && this.drafts().has(r.rowNumber)));
  /** Movimento già esistente per ogni riga da creare che sembra un duplicato. */
  readonly duplicates = computed(() => {
    const result = new Map<number, LegacyDbTransaction>();
    for (const row of this.createRows()) {
      const draft = this.drafts().get(row.rowNumber);
      const duplicate = draft && findDuplicate(row, draft.description, this.dbTransactions());
      if (duplicate) result.set(row.rowNumber, duplicate);
    }
    return result;
  });
  readonly draftsToCreate = computed(() =>
    this.createRows().filter((r) => r.accountId !== null && this.drafts().get(r.rowNumber)?.included),
  );
  readonly dbTransactions = computed(() => this.month()?.dbTransactions ?? []);

  /** Coppie in ordine di riga Excel. */
  readonly pairList = computed(() => [...this.pairs().values()].sort((a, b) => a.rowNumber - b.rowNumber));

  /** Numero progressivo della coppia (badge mostrato su entrambi i lati). */
  readonly pairIndexByRow = computed(() => new Map(this.pairList().map((p, i) => [p.rowNumber, i + 1])));

  readonly rowByTransaction = computed(() => new Map(this.pairList().map((p) => [p.transactionId, p.rowNumber])));

  readonly toSave = computed(() => this.pairList().filter((p) => p.included));

  /** Righe ancora da gestire: né abbinate, né salvate, né segnate come abbinate. */
  readonly pendingRows = computed(() => this.matchRows().filter((r) => !this.isRowHandled(r.rowNumber)));

  private readonly rowsByNumber = computed(() => new Map(this.rows().map((r) => [r.rowNumber, r])));
  private readonly transactionsById = computed(() => new Map(this.dbTransactions().map((t) => [t.id, t])));

  isRowHandled(rowNumber: number) {
    return this.pairs().has(rowNumber) || this.savedRows().has(rowNumber) || this.dismissedRows().has(rowNumber);
  }

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

  /** `openCreateDialog`: false per i ricaricamenti dopo un salvataggio, che non devono riaprire la finestra. */
  async loadSheet(name: string, openCreateDialog = true) {
    const file = this.file();
    if (!file) return;

    this.selectedSheet.set(name);
    this.selectedRow.set(null);
    this.loading.set(true);
    try {
      const month = await this.api.getMonth(file, name);
      // un caricamento più recente (cambio rapido di mese) vince
      if (this.selectedSheet() !== name) return;
      const handled = readHandled(storageKey(file.name, name));
      this.savedRows.set(new Set(handled.saved));
      this.dismissedRows.set(new Set(handled.dismissed));
      this.month.set(month);
      this.pairs.set(this.initialPairs(month));
      this.drafts.set(this.initialDrafts(month));
      // Contanti e Hype non arrivano dagli import: se ce ne sono da creare si apre la finestra dedicata
      if (openCreateDialog) this.createDialogOpen.set(this.drafts().size > 0);
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

  /** Toglie la riga da quelle da gestire senza abbinarla a un movimento (es. già sistemata a mano). */
  markAsMatched(rowNumber: number) {
    this.unpair(rowNumber);
    this.drafts.update((current) => {
      const next = new Map(current);
      next.delete(rowNumber);
      return next;
    });
    this.dismissedRows.update((current) => new Set(current).add(rowNumber));
    if (this.selectedRow() === rowNumber) this.selectedRow.set(null);
    this.persistHandled();
  }

  /** Riporta tra quelle da gestire una riga segnata come abbinata o salvata in questa sessione. */
  restoreRow(rowNumber: number) {
    const without = (current: ReadonlySet<number>) => {
      const next = new Set(current);
      next.delete(rowNumber);
      return next;
    };
    this.dismissedRows.update(without);
    this.savedRows.update(without);
    this.persistHandled();

    const row = this.row(rowNumber);
    if (row?.toCreate && !this.drafts().has(rowNumber)) {
      this.drafts.update((current) =>
        new Map(current).set(rowNumber, {
          rowNumber,
          description: row.description,
          categoryId: row.suggestedCategoryId,
          included: row.accountId !== null && !findDuplicate(row, row.description, this.dbTransactions()),
        }),
      );
    }
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

    const savedRowNumbers = this.toSave().map((p) => p.rowNumber);
    this.saving.set(true);
    try {
      const saved = await this.api.apply(items);
      this.savedRows.update((current) => new Set([...current, ...savedRowNumbers]));
      this.persistHandled();
      this.notifications.showSuccess(`${saved} ${saved === 1 ? "movimento aggiornato" : "movimenti aggiornati"}`);
      this.transactions.notifyExternalChange();
    } catch {
      return; // errore notificato dall'interceptor
    } finally {
      this.saving.set(false);
    }

    const sheet = this.selectedSheet();
    if (sheet) await this.loadSheet(sheet, false);
  }

  updateDraft(rowNumber: number, patch: Partial<Omit<LegacyDraft, "rowNumber">>) {
    this.drafts.update((current) => {
      const draft = current.get(rowNumber);
      if (!draft) return current;
      const next = new Map(current);
      next.set(rowNumber, { ...draft, ...patch });
      return next;
    });
  }

  /**
   * Crea i movimenti delle righe incluse e ricarica il mese. Rilancia l'errore (già notificato) così la
   * finestra resta aperta.
   */
  async createDrafts(): Promise<string | void> {
    const rows = this.draftsToCreate();
    const items: LegacyCreateItem[] = rows.map((row) => {
      const draft = this.drafts().get(row.rowNumber)!;
      return {
        accountId: row.accountId!,
        date: row.date,
        amount: row.amount,
        description: draft.description.trim(),
        categoryId: draft.categoryId,
      };
    });
    if (items.length === 0) return;
    if (items.some((i) => i.description.length === 0)) {
      this.notifications.showError("Ogni movimento deve avere una descrizione");
      throw new Error("empty description");
    }

    this.creating.set(true);
    try {
      const created = await this.api.create(items);
      this.savedRows.update((current) => new Set([...current, ...rows.map((r) => r.rowNumber)]));
      this.persistHandled();
      this.transactions.notifyExternalChange();
      const sheet = this.selectedSheet();
      if (sheet) void this.loadSheet(sheet, false);
      return `${created} ${created === 1 ? "movimento creato" : "movimenti creati"}`;
    } finally {
      this.creating.set(false);
    }
  }

  private persistHandled() {
    const file = this.file();
    const sheet = this.selectedSheet();
    if (!file || !sheet) return;
    writeHandled(storageKey(file.name, sheet), {
      saved: [...this.savedRows()],
      dismissed: [...this.dismissedRows()],
    });
  }

  private initialDrafts(month: LegacyMonth) {
    const drafts = new Map<number, LegacyDraft>();
    for (const row of month.excelRows) {
      if (!row.toCreate || this.savedRows().has(row.rowNumber) || this.dismissedRows().has(row.rowNumber)) continue;
      drafts.set(row.rowNumber, {
        rowNumber: row.rowNumber,
        description: row.description,
        categoryId: row.suggestedCategoryId,
        // un probabile duplicato parte escluso: va incluso consapevolmente
        included: row.accountId !== null && !findDuplicate(row, row.description, month.dbTransactions),
      });
    }
    return drafts;
  }

  private initialPairs(month: LegacyMonth) {
    const rows = new Map(month.excelRows.map((r) => [r.rowNumber, r]));
    const transactions = new Map(month.dbTransactions.map((t) => [t.id, t]));
    const pairs = new Map<number, LegacyPair>();
    for (const suggestion of month.suggestions) {
      // le righe già gestite nella sessione non ricevono più suggerimenti
      if (this.savedRows().has(suggestion.rowNumber) || this.dismissedRows().has(suggestion.rowNumber)) continue;
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
