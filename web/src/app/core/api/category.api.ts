import { Injectable, inject } from "@angular/core";
import { apiConfig } from "../config/api";
import Category from "../models/Category";
import { PagedResult } from "../models/PagedResult";
import { ApiClient } from "./api-client";

export type CategoryPagedResult = PagedResult<Category>;

export interface CategoryQueryRequest {
  name?: string;
  page: number;
  pageSize: number;
}

/** Date yyyy-MM-dd, estremi inclusi. */
export interface CategorySyncRequest {
  startDate: string;
  endDate: string;
}

export interface CategorySyncResult {
  examinedTransactions: number;
  updatedTransactions: number;
  unmatchedTransactions: number;
}

@Injectable({ providedIn: "root" })
export class CategoryApi {
  private readonly api = inject(ApiClient);

  getAll(request: CategoryQueryRequest): Promise<CategoryPagedResult> {
    return this.api.read("POST", apiConfig.categories.query, "Errore nel caricamento delle categorie", request);
  }

  add(category: Category): Promise<void> {
    return this.api.write("POST", apiConfig.categories.base, "Errore nel salvataggio della categoria", [category]);
  }

  update(category: Category): Promise<void> {
    return this.api.write("PUT", apiConfig.categories.base, "Errore nella modifica della categoria", category);
  }

  /** Riassegna la categoria ai movimenti del periodo in base alle parole chiave (trasferimenti esclusi). */
  syncTransactions(request: CategorySyncRequest): Promise<CategorySyncResult> {
    return this.api.read("POST", apiConfig.categories.sync, "Errore nella sincronizzazione delle categorie", request);
  }

  delete(id: number): Promise<void> {
    return this.api.write("DELETE", apiConfig.categories.byId(id), "Errore nell'eliminazione della categoria");
  }
}
