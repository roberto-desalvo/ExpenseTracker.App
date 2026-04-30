import config from "../config/development";
import Transaction from "../models/Transaction";
import { TransactionMonthOption } from "../models/TransactionMonthOption";
import { TransactionQueryRequest } from "../models/TransactionQueryRequest";
import { TransactionQueryResult } from "../models/TransactionQueryResult";
import { apiFetchJson, apiFetchVoid } from "./ApiClient";

const TransactionService = {
  getAll: async (request: TransactionQueryRequest): Promise<TransactionQueryResult> => {
    const url = `${config.expenseTrackerBaseUrl}/${config.expenseTrackerTransactionUrl}/query`;
    const payload: TransactionQueryRequest = {
      ...request,
      idAccounts: request.idAccounts ?? null,
      idCategories: request.idCategories ?? null,
    };
    return apiFetchJson<TransactionQueryResult>(
      url,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      },
      'Errore nel caricamento delle transazioni'
    );
  },

  getMonthOptions: async (): Promise<TransactionMonthOption[]> => {
    const url = `${config.expenseTrackerBaseUrl}/${config.expenseTrackerTransactionUrl}/month-options`;
    return apiFetchJson<TransactionMonthOption[]>(
      url,
      {
        method: "GET",
        headers: { "Content-Type": "application/json" },
      },
      "Errore nel caricamento dei mesi disponibili"
    );
  },

  add: async (transaction: Transaction): Promise<void> => {
    const url = config.expenseTrackerBaseUrl + '/' + config.expenseTrackerTransactionUrl;
    await apiFetchVoid(
      url,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(transaction),
      },
      'Errore nel salvataggio della transazione'
    );
  },

  update: async (transaction: Transaction): Promise<void> => {
    const url = config.expenseTrackerBaseUrl + '/' + config.expenseTrackerTransactionUrl;
    await apiFetchVoid(
      url,
      {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(transaction),
      },
      'Errore nella modifica della transazione'
    );
  },

  delete: async (id: number): Promise<void> => {
    const url = config.expenseTrackerBaseUrl + '/' + config.expenseTrackerTransactionUrl + '/' + id;
    await apiFetchVoid(
      url,
      {
        method: 'DELETE',
        headers: {
          'Content-Type': 'application/json',
        },
      },
      'Errore nell\'eliminazione della transazione'
    );
  },
};

export default TransactionService;
