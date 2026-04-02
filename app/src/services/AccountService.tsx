import config from "../config/development";
import Account from "../models/Account";

const AccountService = {

  getAll: async (): Promise<Account[]> => {
    const url = config.expenseTrackerBaseUrl + '/' + config.expenseTrackerAccountUrl;
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error('Errore while loading accounts');
    }
    return response.json();
  },
};

export default AccountService;
