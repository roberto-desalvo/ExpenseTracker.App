import { Injectable, inject } from "@angular/core";
import { apiConfig } from "../config/api";
import { LandingDashboard } from "../models/LandingDashboard";
import { PagedResult } from "../models/PagedResult";
import { TimeSeriesList } from "../models/TimeSeries";
import { TimeSeriesRequest } from "../models/TimeSeriesRequest";
import Transaction from "../models/Transaction";
import { TransactionMonthOption } from "../models/TransactionMonthOption";
import { TransactionQueryRequest } from "../models/TransactionQueryRequest";
import { TransferPayload } from "../models/Transfer";
import { ApiClient } from "./api-client";

const normalizeSeriesRequest = (request: TimeSeriesRequest): TimeSeriesRequest => ({
  ...request,
  idAccounts: request.idAccounts && request.idAccounts.length > 0 ? request.idAccounts : [],
  idCategories: request.idCategories && request.idCategories.length > 0 ? request.idCategories : [],
  excludeTransfers: request.excludeTransfers ?? false,
});

@Injectable({ providedIn: "root" })
export class TransactionApi {
  private readonly api = inject(ApiClient);

  getAll(request: TransactionQueryRequest): Promise<PagedResult<Transaction>> {
    const payload: TransactionQueryRequest = {
      ...request,
      idAccounts: request.idAccounts ?? null,
      idCategories: request.idCategories ?? null,
    };
    return this.api.read("POST", apiConfig.transactions.query, "Errore nel caricamento delle transazioni", payload);
  }

  getMonthOptions(): Promise<TransactionMonthOption[]> {
    return this.api.read("GET", apiConfig.transactions.monthOptions, "Errore nel caricamento dei mesi disponibili");
  }

  getLanding({ excludeTransfers = true }: { excludeTransfers?: boolean } = {}): Promise<LandingDashboard> {
    return this.api.read(
      "GET",
      `${apiConfig.transactions.landing}?excludeTransfers=${excludeTransfers}`,
      "Errore nel caricamento della dashboard",
    );
  }

  getTimeSeries(request: TimeSeriesRequest): Promise<TimeSeriesList> {
    return this.api.read(
      "POST",
      apiConfig.transactions.series,
      "Errore nel caricamento della serie temporale",
      normalizeSeriesRequest(request),
    );
  }

  getStock(request: TimeSeriesRequest): Promise<TimeSeriesList> {
    return this.api.read(
      "POST",
      apiConfig.transactions.stock,
      "Errore nel caricamento dello stock",
      normalizeSeriesRequest(request),
    );
  }

  add(transaction: Transaction): Promise<void> {
    return this.api.write("POST", apiConfig.transactions.base, "Errore nel salvataggio della transazione", transaction);
  }

  update(transaction: Transaction): Promise<void> {
    return this.api.write("PUT", apiConfig.transactions.base, "Errore nella modifica della transazione", transaction);
  }

  delete(id: number): Promise<void> {
    return this.api.write("DELETE", apiConfig.transactions.byId(id), "Errore nell'eliminazione della transazione");
  }

  deleteMany(ids: number[]): Promise<void> {
    return this.api.write("POST", apiConfig.transactions.bulkDelete, "Errore nell'eliminazione delle transazioni", { ids });
  }

  /** Scompone il movimento in più voci con gli importi indicati (somma = importo originale; il primo resta sull'originale). */
  split(id: number, amounts: number[]): Promise<void> {
    return this.api.write("POST", apiConfig.transactions.split(id), "Errore nella scomposizione della transazione", { amounts });
  }

  /** categoryId 0 = nessuna categoria */
  assignCategory(ids: number[], categoryId: number): Promise<void> {
    return this.api.write("POST", apiConfig.transactions.bulkCategory, "Errore nell'assegnazione della categoria", {
      ids,
      categoryId,
    });
  }

  /** Movimenti che possono completare il trasferimento del movimento indicato (stesso mese, importo opposto). */
  getTransferCandidates(id: number): Promise<Transaction[]> {
    return this.api.read(
      "GET",
      apiConfig.transactions.transferCandidates(id),
      "Errore nel caricamento dei movimenti corrispondenti",
    );
  }

  /** Collega `counterpartId` al trasferimento di `id`; la sua data diventa quella di `id`. */
  linkTransferCounterpart(id: number, counterpartId: number): Promise<void> {
    return this.api.write(
      "POST",
      apiConfig.transactions.transferCounterpart(id),
      "Errore nel collegamento del trasferimento",
      { counterpartId },
    );
  }

  /** Crea il ramo mancante del trasferimento di `id` sul conto indicato (importo opposto, stessa data). */
  createTransferCounterpart(id: number, newCounterpart: { accountId: number; description: string }): Promise<void> {
    return this.api.write(
      "POST",
      apiConfig.transactions.transferCounterpart(id),
      "Errore nella creazione del movimento corrispondente",
      { newCounterpart },
    );
  }

  addTransfer(transfer: TransferPayload): Promise<void> {
    return this.api.write("POST", apiConfig.transfers.base, "Errore nel salvataggio del trasferimento", transfer);
  }
}
