import { Injectable, inject } from "@angular/core";
import { AccountApi } from "../api/account.api";
import Account from "../models/Account";
import { PagedEntityStore, PagedQuery } from "./paged-entity.store";

/** Conti: `all` = elenco completo, `paged` = lista della vista Impostazioni → Conti. */
@Injectable({ providedIn: "root" })
export class AccountsStore extends PagedEntityStore<Account> {
  private readonly api = inject(AccountApi);

  protected query(request: PagedQuery) {
    return this.api.query(request);
  }

  async addAccount(account: Account) {
    await this.api.add(account);
    await this.reloadFromFirstPage();
  }

  async updateAccount(account: Account) {
    await this.api.update(account);
    await this.reloadBoth();
  }
}
