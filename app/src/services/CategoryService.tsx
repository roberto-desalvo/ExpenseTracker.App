import config from "../config/development";
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
    const url = `${config.expenseTrackerBaseUrl}/${config.expenseTrackerCategoryUrl}/query`;
    return apiFetchJson<CategoryPagedResult>(
      url,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(request),
      },
      "Errore nel caricamento delle categorie"
    );
  },

  add: async (category: Category): Promise<void> => {
    const url = `${config.expenseTrackerBaseUrl}/${config.expenseTrackerCategoryUrl}`;
    await apiFetchVoid(
      url,
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
    const url = `${config.expenseTrackerBaseUrl}/${config.expenseTrackerCategoryUrl}`;
    await apiFetchVoid(
      url,
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
    const url = `${config.expenseTrackerBaseUrl}/${config.expenseTrackerCategoryUrl}/${id}`;
    await apiFetchVoid(
      url,
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
