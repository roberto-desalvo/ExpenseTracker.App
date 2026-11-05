import { Injectable, computed, inject, signal } from "@angular/core";
import { ImportApi } from "../api/import.api";
import { NotificationService } from "../api/notification.service";
import { AuthService } from "../auth/auth.service";
import Account from "../models/Account";
import { ImportLog, isImportActive } from "../models/ImportLog";
import { AccountsStore } from "./accounts.store";
import { AccountTypesStore } from "./account-types.store";
import { TransactionsStore } from "./transactions.store";

/** Intervallo di aggiornamento mentre ci sono import in coda o in elaborazione. */
export const IMPORT_POLL_INTERVAL_MS = 4000;

/**
 * Import da file: conti importabili dell'utente, caricamento asincrono e storico (ImportLogs).
 * Finché ci sono import in corso lo store li ricontrolla periodicamente (anche fuori dalla pagina Import, per quelli
 * caricati in questa sessione): quando uno si conclude notifica l'esito e, se ha importato movimenti, aggiorna le viste.
 */
@Injectable({ providedIn: "root" })
export class ImportsStore {
  private readonly api = inject(ImportApi);
  private readonly accounts = inject(AccountsStore);
  private readonly accountTypes = inject(AccountTypesStore);
  private readonly auth = inject(AuthService);
  private readonly transactions = inject(TransactionsStore);
  private readonly notifications = inject(NotificationService);

  /** Conti dell'utente di un tipo importabile; l'utente demo non può importare file. */
  readonly importableAccounts = computed<Account[]>(() => {
    if (this.auth.user()?.mode === "demo") return [];
    const types = this.accountTypes.byId();
    return this.accounts.all().filter((a) => a.accountTypeId != null && types.get(a.accountTypeId)?.supportsImport === true);
  });
  readonly canImport = computed(() => this.importableAccounts().length > 0);

  readonly logs = signal<ImportLog[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(0);
  readonly pageSize = signal(20);
  readonly isLoading = signal(false);
  readonly activeCount = computed(() => this.logs().filter(isImportActive).length);

  private pageActive = false;
  /** import visti in coda/in elaborazione, di cui notificare l'esito */
  private readonly pending = new Map<number, ImportLog>();
  private timer: ReturnType<typeof setTimeout> | null = null;
  private requestId = 0;

  /** Chiamata dalla pagina Import all'apertura; restituisce la funzione di disattivazione. */
  activate(): () => void {
    this.pageActive = true;
    void this.load(true);
    return () => {
      this.pageActive = false;
      if (this.pending.size === 0) this.stopPolling();
    };
  }

  modifyPage(page: number) {
    this.page.set(page);
    void this.load(true);
  }

  modifyPageSize(pageSize: number) {
    this.pageSize.set(pageSize);
    this.page.set(0);
    void this.load(true);
  }

  /** Carica il file e lo mette in coda; l'esito arriva poi con il polling. */
  async upload(accountId: number, file: File): Promise<ImportLog> {
    const log = await this.api.upload(accountId, file);
    this.pending.set(log.id, log);
    if (this.pageActive) {
      this.page.set(0);
      await this.load(false);
    } else {
      this.schedulePoll();
    }
    return log;
  }

  private async load(showLoading: boolean) {
    this.stopPolling();
    const id = ++this.requestId;
    if (showLoading) this.isLoading.set(true);
    try {
      const result = await this.api.query({ page: this.pageActive ? this.page() : 0, pageSize: this.pageSize() });
      if (id !== this.requestId) return;
      if (this.pageActive) {
        this.logs.set(result.items);
        this.totalCount.set(result.totalCount);
      }
      this.track(result.items);
      this.schedulePoll();
    } catch {
      // errore mostrato dal layer API: il polling si ferma e riparte alla prossima apertura della pagina
    } finally {
      if (id === this.requestId && showLoading) this.isLoading.set(false);
    }
  }

  private track(items: ImportLog[]) {
    let importedTransactions = false;
    for (const item of items) {
      if (isImportActive(item)) {
        this.pending.set(item.id, item);
        continue;
      }
      if (!this.pending.delete(item.id)) continue;

      const file = item.fileName ? `“${item.fileName}”` : "del file";
      if (item.status === "Completed") {
        importedTransactions ||= item.newTransactionsCount > 0;
        this.notifications.showSuccess(`Import ${file} completato: ${item.newTransactionsCount} nuovi movimenti`);
      } else {
        this.notifications.showError(`Import ${file} non riuscito${item.errorMessage ? `: ${item.errorMessage}` : ""}`);
      }
    }
    if (importedTransactions) this.transactions.notifyExternalChange();
  }

  private schedulePoll() {
    this.stopPolling();
    if (this.pending.size === 0) return;
    this.timer = setTimeout(() => void this.load(false), IMPORT_POLL_INTERVAL_MS);
  }

  private stopPolling() {
    if (this.timer !== null) {
      clearTimeout(this.timer);
      this.timer = null;
    }
  }
}
