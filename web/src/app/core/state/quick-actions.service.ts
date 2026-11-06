import { Injectable, signal } from "@angular/core";
import Transaction from "../models/Transaction";

const cloneTransaction = (transaction: Transaction) =>
  new Transaction(
    transaction.id,
    new Date(transaction.date),
    transaction.description,
    transaction.amount,
    transaction.categoryId,
    transaction.category,
    transaction.accountId,
    transaction.account,
  );

const createEmptyTransaction = () => new Transaction(0, new Date(), "", 0, 0, "", 0, "");

/**
 * Apertura delle modali di inserimento (transazione e trasferimento), montate una sola volta nel layout
 * e apribili da qualunque pagina (header, dashboard, movimenti).
 */
@Injectable({ providedIn: "root" })
export class QuickActionsService {
  readonly transactionModalOpen = signal(false);
  /** copia della transazione in modifica (id 0 = nuova) */
  readonly currentTransaction = signal<Transaction | null>(null);
  readonly transferModalOpen = signal(false);

  openTransactionModal(transaction?: Transaction | null) {
    this.currentTransaction.set(transaction ? cloneTransaction(transaction) : createEmptyTransaction());
    this.transactionModalOpen.set(true);
  }

  /** Apre l'inserimento di una nuova transazione prepopolata con i dati di quella indicata. */
  openDuplicateTransaction(transaction: Transaction) {
    this.currentTransaction.set(Object.assign(cloneTransaction(transaction), { id: 0 }));
    this.transactionModalOpen.set(true);
  }

  closeTransactionModal() {
    this.transactionModalOpen.set(false);
  }

  openNewTransaction() {
    this.openTransactionModal();
  }

  openNewTransfer() {
    this.transferModalOpen.set(true);
  }

  closeTransferModal() {
    this.transferModalOpen.set(false);
  }
}
