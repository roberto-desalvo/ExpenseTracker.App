import { apiConfig } from "../config/api";
import Account from "../models/Account";
import { apiFetchJson, apiFetchVoid } from "./ApiClient";

export interface AccountPagedResult {
  items: Account[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AccountQueryRequest {
  name?: string;
  page: number;
  pageSize: number;
}

const AccountService = {
  query: async (request: AccountQueryRequest): Promise<AccountPagedResult> => {
    return apiFetchJson<AccountPagedResult>(
      apiConfig.accounts.query,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(request),
      },
      "Errore nel caricamento degli account"
    );
  },

  add: async (account: Account): Promise<void> => {
    await apiFetchVoid(
      apiConfig.accounts.base,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify([account]),
      },
      "Errore nel salvataggio dell'account"
    );
  },

  update: async (account: Account): Promise<void> => {
    await apiFetchVoid(
      apiConfig.accounts.base,
      {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(account),
      },
      "Errore nella modifica dell'account"
    );
  },
};

export default AccountService;
