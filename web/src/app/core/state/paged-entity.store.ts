import { signal } from "@angular/core";
import { PagedResult } from "../models/PagedResult";

export interface PagedQuery {
  name?: string;
  page: number;
  pageSize: number;
}

/**
 * Base comune di conti e categorie:
 * - elenco completo, caricato una sola volta e condiviso da tutte le pagine (`all`);
 * - lista paginata e filtrata per nome (`paged`), attiva solo mentre la vista che la mostra è visibile.
 */
export abstract class PagedEntityStore<T> {
  readonly all = signal<T[]>([]);
  /** true dopo il primo caricamento (anche fallito) dell'elenco completo */
  readonly loaded = signal(false);
  readonly paged = signal<T[]>([]);
  readonly isLoading = signal(false);
  readonly page = signal(0);
  readonly pageSize = signal(10);
  readonly totalCount = signal(0);

  private pagedListActive = false;
  private loadingAll = false;
  private currentName: string | undefined;
  private pagedRequestId = 0;

  protected abstract query(request: PagedQuery): Promise<PagedResult<T>>;

  /** Carica l'elenco completo se non è già stato caricato. */
  ensureLoaded() {
    if (this.loaded() || this.loadingAll) return;
    this.refreshAll().catch(() => {
      // gestito dall'error layer
    });
  }

  /** Attiva la lista paginata finché la vista che la mostra è montata; restituisce la funzione di disattivazione. */
  activatePagedList(): () => void {
    this.pagedListActive = true;
    this.loadPaged();
    return () => {
      this.pagedListActive = false;
      this.currentName = undefined;
      this.page.set(0);
    };
  }

  modifyPage(newPage: number) {
    this.page.set(newPage);
    this.loadPaged();
  }

  modifyPageSize(newPageSize: number) {
    this.pageSize.set(newPageSize);
    this.page.set(0);
    this.loadPaged();
  }

  /** Applica il filtro per nome alla lista paginata, ripartendo dalla prima pagina. */
  refreshPaged(name?: string) {
    this.currentName = name;
    this.page.set(0);
    this.loadPaged();
  }

  /** Dopo una creazione: torna alla prima pagina e ricarica entrambi gli elenchi. */
  protected async reloadFromFirstPage() {
    this.page.set(0);
    await this.reloadBoth();
  }

  /** Dopo una modifica/eliminazione: ricarica la pagina corrente e l'elenco completo. */
  protected async reloadBoth() {
    await Promise.all([this.refreshAll(), this.loadPagedItems()]);
  }

  private async refreshAll() {
    this.loadingAll = true;
    try {
      const result = await this.query({ page: 0, pageSize: 10000 });
      this.all.set(result.items);
    } finally {
      this.loadingAll = false;
      this.loaded.set(true);
    }
  }

  private loadPaged() {
    this.loadPagedItems().catch(() => {
      // gestito dall'error layer
    });
  }

  private async loadPagedItems() {
    if (!this.pagedListActive) return;
    // solo l'ultima richiesta aggiorna lo stato: risposte arrivate fuori ordine vengono ignorate
    const requestId = ++this.pagedRequestId;
    this.isLoading.set(true);
    try {
      const result = await this.query({ name: this.currentName, page: this.page(), pageSize: this.pageSize() });
      if (requestId !== this.pagedRequestId) return;
      this.paged.set(result.items);
      this.totalCount.set(result.totalCount);
    } finally {
      if (requestId === this.pagedRequestId) this.isLoading.set(false);
    }
  }
}
