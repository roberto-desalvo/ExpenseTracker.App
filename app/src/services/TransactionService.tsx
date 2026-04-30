import config from "../config/development";
import Transaction from "../models/Transaction";
import { TransactionMonthOption } from "../models/TransactionMonthOption";
import { TransactionQueryRequest } from "../models/TransactionQueryRequest";
import { TransactionQueryResult } from "../models/TransactionQueryResult";
import { TimeSeriesList } from "../models/TimeSeries";
import { TimeSeriesRequest } from "../models/TimeSeriesRequest";
import { LandingDashboard } from "../models/LandingDashboard";
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

  getLanding: async (): Promise<LandingDashboard> => {
    const url = `${config.expenseTrackerBaseUrl}/${config.expenseTrackerTransactionUrl}/landing`;
    return apiFetchJson<LandingDashboard>(
      url,
      {
        method: "GET",
        headers: { "Content-Type": "application/json" },
      },
      "Errore nel caricamento della dashboard"
    );
  },

  getTimeSeries: async (request: TimeSeriesRequest): Promise<TimeSeriesList> => {
    const url = `${config.expenseTrackerBaseUrl}/${config.expenseTrackerTransactionUrl}/series`;
    const payload: TimeSeriesRequest = {
      ...request,
      idAccounts: request.idAccounts && request.idAccounts.length > 0 ? request.idAccounts : [],
      idCategories: request.idCategories && request.idCategories.length > 0 ? request.idCategories : [],
    };

    return apiFetchJson<TimeSeriesList>(
      url,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      },
      "Errore nel caricamento della serie temporale"
    );
  },

  getStock: async (request: TimeSeriesRequest): Promise<TimeSeriesList> => {
    const url = `${config.expenseTrackerBaseUrl}/${config.expenseTrackerTransactionUrl}/stock`;
    const payload: TimeSeriesRequest = {
      ...request,
      idAccounts: request.idAccounts && request.idAccounts.length > 0 ? request.idAccounts : [],
      idCategories: request.idCategories && request.idCategories.length > 0 ? request.idCategories : [],
    };

    return apiFetchJson<TimeSeriesList>(
      url,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      },
      "Errore nel caricamento dello stock"
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
