import config from "../config/development";
import Category from "../models/Category";
import { apiFetchJson, apiFetchVoid } from "./ApiClient";

const buildCategoryUrl = (name?: string) => {
  const baseUrl = `${config.expenseTrackerBaseUrl}/${config.expenseTrackerCategoryUrl}`;

  if (!name || name.trim().length === 0) {
    return baseUrl;
  }

  const params = new URLSearchParams({ name: name.trim() });
  return `${baseUrl}?${params.toString()}`;
};

const CategoryService = {
  getAll: async (name?: string): Promise<Category[]> => {
    const url = buildCategoryUrl(name);
    return apiFetchJson<Category[]>(
      url,
      { method: "GET" },
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
