import config from "../config/development";
import Account from "../models/Account";
import { apiFetchJson } from "./ApiClient";

const AccountService = {

  getAll: async (): Promise<Account[]> => {
    const url = config.expenseTrackerBaseUrl + '/' + config.expenseTrackerAccountUrl;
    return apiFetchJson<Account[]>(
      url,
      { method: "GET" },
      "Errore nel caricamento degli account"
    );
  },
};

export default AccountService;
