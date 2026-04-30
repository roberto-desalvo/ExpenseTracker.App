import React, { createContext, ReactNode, useContext, useEffect, useState } from "react";
import Category from "../models/Category";
import CategoryService from "../services/CategoryService";

interface CategoriesContextType {
  categories: Category[];
  isLoading: boolean;
  addCategory: (category: Category) => Promise<void>;
  updateCategory: (category: Category) => Promise<void>;
  deleteCategory: (id: number) => Promise<void>;
  refreshCategories: (name?: string) => Promise<void>;
}

const CategoriesContext = createContext<CategoriesContextType | undefined>(
  undefined
);

export const CategoriesProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [categories, setCategories] = useState<Category[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);

  const refreshCategories = async (name?: string) => {
    setIsLoading(true);
    try {
      const result = await CategoryService.getAll(name);
      setCategories(result);
    } catch (error) {
      console.error("Errore nel caricamento delle categorie:", error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void refreshCategories();
  }, []);

  const addCategory = async (category: Category) => {
    try {
      await CategoryService.add(category);
      await refreshCategories();
    } catch (error) {
      console.error("Errore nell'aggiunta della categoria:", error);
      throw error;
    }
  };

  const updateCategory = async (category: Category) => {
    try {
      await CategoryService.update(category);
      await refreshCategories();
    } catch (error) {
      console.error("Errore nella modifica della categoria:", error);
      throw error;
    }
  };

  const deleteCategory = async (id: number) => {
    try {
      await CategoryService.delete(id);
      await refreshCategories();
    } catch (error) {
      console.error("Errore nell'eliminazione della categoria:", error);
      throw error;
    }
  };

  return (
    <CategoriesContext.Provider
      value={{
        categories,
        isLoading,
        addCategory,
        updateCategory,
        deleteCategory,
        refreshCategories,
      }}
    >
      {children}
    </CategoriesContext.Provider>
  );
};

export const useCategories = (): CategoriesContextType => {
  const context = useContext(CategoriesContext);
  if (!context) {
    throw new Error(
      "useCategories deve essere utilizzato all’interno di CategoriesProvider"
    );
  }
  return context;
};
