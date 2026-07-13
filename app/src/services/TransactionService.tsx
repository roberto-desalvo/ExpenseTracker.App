import { apiConfig } from "../config/api";
import Transaction from "../models/Transaction";
import { TransactionMonthOption } from "../models/TransactionMonthOption";
import { TransactionQueryRequest } from "../models/TransactionQueryRequest";
import { TransactionQueryResult } from "../models/TransactionQueryResult";
import { TimeSeriesList } from "../models/TimeSeries";
import { TimeSeriesRequest } from "../models/TimeSeriesRequest";
import { LandingDashboard } from "../models/LandingDashboard";
import { apiFetchJson, apiFetchVoid } from "./ApiClient";

interface LandingRequestOptions {
  excludeTransfers?: boolean;
}

const TransactionService = {
  getAll: async (request: TransactionQueryRequest): Promise<TransactionQueryResult> => {
    const payload: TransactionQueryRequest = {
      ...request,
      idAccounts: request.idAccounts ?? null,
      idCategories: request.idCategories ?? null,
    };
    return apiFetchJson<TransactionQueryResult>(
      apiConfig.transactions.query,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      },
      'Errore nel caricamento delle transazioni'
    );
  },

  getMonthOptions: async (): Promise<TransactionMonthOption[]> => {
    return apiFetchJson<TransactionMonthOption[]>(
      apiConfig.transactions.monthOptions,
      {
        method: "GET",
        headers: { "Content-Type": "application/json" },
      },
      "Errore nel caricamento dei mesi disponibili"
    );
  },

  getLanding: async ({ excludeTransfers = true }: LandingRequestOptions = {}): Promise<LandingDashboard> => {
    const landingUrl = `${apiConfig.transactions.landing}?excludeTransfers=${excludeTransfers}`;
    return apiFetchJson<LandingDashboard>(
      landingUrl,
      {
        method: "GET",
        headers: { "Content-Type": "application/json" },
      },
      "Errore nel caricamento della dashboard"
    );
  },

  getTimeSeries: async (request: TimeSeriesRequest): Promise<TimeSeriesList> => {
    const payload: TimeSeriesRequest = {
      ...request,
      idAccounts: request.idAccounts && request.idAccounts.length > 0 ? request.idAccounts : [],
      idCategories: request.idCategories && request.idCategories.length > 0 ? request.idCategories : [],
      excludeTransfers: request.excludeTransfers ?? false,
    };

    return apiFetchJson<TimeSeriesList>(
      apiConfig.transactions.series,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      },
      "Errore nel caricamento della serie temporale"
    );
  },

  getStock: async (request: TimeSeriesRequest): Promise<TimeSeriesList> => {
    const payload: TimeSeriesRequest = {
      ...request,
      idAccounts: request.idAccounts && request.idAccounts.length > 0 ? request.idAccounts : [],
      idCategories: request.idCategories && request.idCategories.length > 0 ? request.idCategories : [],
      excludeTransfers: request.excludeTransfers ?? false,
    };

    return apiFetchJson<TimeSeriesList>(
      apiConfig.transactions.stock,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      },
      "Errore nel caricamento dello stock"
    );
  },

  add: async (transaction: Transaction): Promise<void> => {
    await apiFetchVoid(
      apiConfig.transactions.base,
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
    await apiFetchVoid(
      apiConfig.transactions.base,
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
    await apiFetchVoid(
      apiConfig.transactions.byId(id),
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
