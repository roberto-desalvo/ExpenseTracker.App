import { Injectable, inject } from "@angular/core";
import { apiConfig } from "../config/api";
import { AccountType } from "../models/AccountType";
import { ApiClient } from "./api-client";

@Injectable({ providedIn: "root" })
export class AccountTypeApi {
  private readonly api = inject(ApiClient);

  getAll(): Promise<AccountType[]> {
    return this.api.read("GET", apiConfig.accountTypes.base, "Errore nel caricamento dei tipi di conto");
  }
}
