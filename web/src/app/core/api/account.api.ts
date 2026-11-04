import { Injectable, inject } from "@angular/core";
import { apiConfig } from "../config/api";
import Account from "../models/Account";
import { PagedResult } from "../models/PagedResult";
import { ApiClient } from "./api-client";

export type AccountPagedResult = PagedResult<Account>;

export interface AccountQueryRequest {
  name?: string;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: "root" })
export class AccountApi {
  private readonly api = inject(ApiClient);

  query(request: AccountQueryRequest): Promise<AccountPagedResult> {
    return this.api.read("POST", apiConfig.accounts.query, "Errore nel caricamento degli account", request);
  }

  add(account: Account): Promise<void> {
    return this.api.write("POST", apiConfig.accounts.base, "Errore nel salvataggio dell'account", [account]);
  }

  update(account: Account): Promise<void> {
    return this.api.write("PUT", apiConfig.accounts.base, "Errore nella modifica dell'account", account);
  }
}
