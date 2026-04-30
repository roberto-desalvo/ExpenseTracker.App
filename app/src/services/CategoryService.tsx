import { apiConfig } from "../config/api";
import Category from "../models/Category";
import { apiFetchJson, apiFetchVoid } from "./ApiClient";

export interface CategoryPagedResult {
  items: Category[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CategoryQueryRequest {
  name?: string;
  page: number;
  pageSize: number;
}

const CategoryService = {
  getAll: async (request: CategoryQueryRequest): Promise<CategoryPagedResult> => {
    return apiFetchJson<CategoryPagedResult>(
      apiConfig.categories.query,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(request),
      },
      "Errore nel caricamento delle categorie"
    );
  },

  add: async (category: Category): Promise<void> => {
    await apiFetchVoid(
      apiConfig.categories.base,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify([category]),
      },
      "Errore nel salvataggio della categoria"
    );
  },

  update: async (category: Category): Promise<void> => {
    await apiFetchVoid(
      apiConfig.categories.base,
      {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(category),
      },
      "Errore nella modifica della categoria"
    );
  },

  delete: async (id: number): Promise<void> => {
    await apiFetchVoid(
      apiConfig.categories.byId(id),
      {
        method: "DELETE",
        headers: {
          "Content-Type": "application/json",
        },
      },
      "Errore nell'eliminazione della categoria"
    );
  },
};

export default CategoryService;
