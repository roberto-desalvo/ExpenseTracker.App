import config from "../config/development";
import Category from "../models/Category";
import { apiFetchJson } from "./ApiClient";

const CategoryService = {
  getAll: async (): Promise<Category[]> => {
    const url = config.expenseTrackerBaseUrl + '/' + config.expenseTrackerCategoryUrl;
    return apiFetchJson<Category[]>(
      url,
      { method: "GET" },
      "Errore nel caricamento delle categorie"
    );
  },
};

export default CategoryService;
