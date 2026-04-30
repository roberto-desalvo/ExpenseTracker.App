import config from "../config/development";
import { PagedResult } from "../models/PagedResult";
import Transaction from "../models/Transaction";
import { TransactionMonthOption } from "../models/TransactionMonthOption";
import { TransactionQueryRequest } from "../models/TransactionQueryRequest";

const TransactionService = {
  getAll: async (request: TransactionQueryRequest): Promise<PagedResult<Transaction>> => {
    const url = `${config.expenseTrackerBaseUrl}/${config.expenseTrackerTransactionUrl}/query`;
    const payload: TransactionQueryRequest = {
      ...request,
      idAccounts: request.idAccounts ?? null,
      idCategories: request.idCategories ?? null,
    };
    const response = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    if (!response.ok) {
      throw new Error('Errore while loading transactions');
    }
    return response.json();
  },

  getMonthOptions: async (): Promise<TransactionMonthOption[]> => {
    const url = `${config.expenseTrackerBaseUrl}/${config.expenseTrackerTransactionUrl}/month-options`;
    const response = await fetch(url, {
      method: "GET",
      headers: { "Content-Type": "application/json" },
    });
    if (!response.ok) {
      throw new Error("Errore while loading transaction month options");
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
