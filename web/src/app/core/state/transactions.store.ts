import { Injectable, inject, signal } from "@angular/core";
import { TransactionApi } from "../api/transaction.api";
import Transaction from "../models/Transaction";
import { TransactionMonthOption } from "../models/TransactionMonthOption";
import { TransactionQueryRequest } from "../models/TransactionQueryRequest";
import { TransferPayload } from "../models/Transfer";

/**
 * Movimenti: lista paginata della pagina Movimenti, mesi disponibili e operazioni di scrittura.
 * `dataVersion` è incrementato a ogni modifica: le viste aggregate (dashboard, analisi) lo osservano per ricaricarsi.
 */
@Injectable({ providedIn: "root" })
export class TransactionsStore {
  private readonly api = inject(TransactionApi);

  readonly transactions = signal<Transaction[]>([]);
  readonly isLoading = signal(false);
  readonly totalCount = signal(0);
  readonly availableMonths = signal<TransactionMonthOption[]>([]);
  readonly availableMonthsLoading = signal(true);
  readonly dataVersion = signal(0);

  private lastRequest: TransactionQueryRequest | null = null;
  private requestId = 0;
  /** true mentre la pagina Movimenti è visibile: solo lì servono mesi e lista. */
  private pageActive = false;

  /** Chiamata dalla pagina Movimenti all'apertura; restituisce la funzione di disattivazione. */
  activatePage(): () => void {
    this.pageActive = true;
    void this.loadAvailableMonths();
    return () => {
      this.pageActive = false;
    };
  }

  refreshTransactions(request?: TransactionQueryRequest) {
    const effectiveRequest = request ?? this.lastRequest;
    if (!effectiveRequest) return;
    if (request) this.lastRequest = request;

    const id = ++this.requestId;
    this.isLoading.set(true);
    this.api
      .getAll(effectiveRequest)
      .then((result) => {
        if (id !== this.requestId) return;
        this.transactions.set(result.items);
        this.totalCount.set(result.totalCount);
      })
      .catch((error) => console.error("Errore nel caricamento delle transactions:", error))
      .finally(() => {
        if (id === this.requestId) this.isLoading.set(false);
      });
  }

  async addTransaction(transaction: Transaction) {
    await this.api.add(transaction);
    await this.reloadAfterMutation();
  }

  async updateTransaction(transaction: Transaction) {
    await this.api.update(transaction);
    await this.reloadAfterMutation();
  }

  /** Divide il movimento in due con metà importo ciascuno; in caso di errore rilancia. */
  async splitTransaction(id: number) {
    await this.api.split(id);
    await this.reloadAfterMutation();
  }

  async addTransfer(transfer: TransferPayload) {
    await this.api.addTransfer(transfer);
    await this.reloadAfterMutation();
  }

  async deleteTransaction(transaction: Transaction) {
    try {
      await this.api.delete(transaction.id);
      await this.reloadAfterMutation();
    } catch (error) {
      console.error("Errore nell'eliminazione della transaction:", error);
    }
  }

  /** Operazioni massive: in caso di errore rilanciano, così la modale di conferma resta aperta. */
  async deleteTransactions(ids: number[]) {
    await this.api.deleteMany(ids);
    await this.reloadAfterMutation();
  }

  async assignCategory(ids: number[], categoryId: number | null) {
    await this.api.assignCategory(ids, categoryId ?? 0);
    await this.reloadAfterMutation();
  }

  async linkTransferCounterpart(id: number, counterpartId: number) {
    await this.api.linkTransferCounterpart(id, counterpartId);
    await this.reloadAfterMutation();
  }

  async createTransferCounterpart(id: number, accountId: number, description: string) {
    await this.api.createTransferCounterpart(id, { accountId, description });
    await this.reloadAfterMutation();
  }

  private async loadAvailableMonths() {
    this.availableMonthsLoading.set(true);
    try {
      this.availableMonths.set(await this.api.getMonthOptions());
    } catch (error) {
      console.error("Errore nel caricamento dei mesi disponibili:", error);
      this.availableMonths.set([]);
    } finally {
      this.availableMonthsLoading.set(false);
    }
  }

  /** Movimenti cambiati fuori da questo store (es. un import da file concluso): ricarica le viste interessate. */
  notifyExternalChange() {
    void this.reloadAfterMutation();
  }

  /**
   * Dopo una modifica: le viste aggregate si ricaricano tramite `dataVersion`;
   * mesi e lista dei movimenti solo se la pagina Movimenti è aperta (altrimenti li ricarica l'atterraggio sulla pagina).
   */
  private async reloadAfterMutation() {
    this.dataVersion.update((v) => v + 1);
    if (!this.pageActive) return;
    await this.loadAvailableMonths();
    this.refreshTransactions();
  }
}
