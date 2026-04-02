import config from "../config/development";
import Transaction from "../models/Transaction";

const TransactionService = {
  getAll: async (): Promise<Transaction[]> => {
    const url = config.expenseTrackerBaseUrl + '/' + config.expenseTrackerTransactionUrl;
    const response = await fetch(url + "?fromdate=2024-01-01");
    if (!response.ok) {
      throw new Error('Errore while loading transactions');
    }
    return response.json();
  },

  add: async (transaction: Transaction): Promise<void> => {
    const url = config.expenseTrackerBaseUrl + '/' + config.expenseTrackerTransactionUrl;
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(transaction),
    });
    if (!response.ok) {
      throw new Error('Errore while adding transaction');
    }
  },

  delete: async (id: number): Promise<void> => {
    const url = config.expenseTrackerBaseUrl + '/' + config.expenseTrackerTransactionUrl + '/' + id;
    const response = await fetch(url, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
      },
    });
    if (!response.ok) {
      throw new Error('Errore while deleting transaction');
    }
  },
};

export default TransactionService;
