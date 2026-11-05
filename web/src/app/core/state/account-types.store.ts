import { Injectable, computed, inject, signal } from "@angular/core";
import { AccountTypeApi } from "../api/account-type.api";
import { AccountType } from "../models/AccountType";

/** Catalogo dei tipi di conto: fisso, caricato una sola volta. */
@Injectable({ providedIn: "root" })
export class AccountTypesStore {
  private readonly api = inject(AccountTypeApi);
  private loading = false;

  readonly all = signal<AccountType[]>([]);
  readonly loaded = signal(false);
  readonly byId = computed(() => new Map(this.all().map((t) => [t.id, t])));

  ensureLoaded() {
    if (this.loaded() || this.loading) return;
    this.loading = true;
    this.api
      .getAll()
      .then((types) => {
        this.all.set(types);
        this.loaded.set(true);
      })
      .catch(() => {
        // errore mostrato dal layer API; si riprova alla prossima richiesta
      })
      .finally(() => (this.loading = false));
  }
}
