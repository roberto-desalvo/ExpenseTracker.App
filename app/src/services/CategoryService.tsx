import config from "../config/development";
import Category from "../models/Category";

const CategoryService = {
  getAll: async (): Promise<Category[]> => {
    const url = config.expenseTrackerBaseUrl + '/' + config.expenseTrackerCategoryUrl;
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error('Error while loading categories');
    }
    return response.json();
  },
};

export default CategoryService;
