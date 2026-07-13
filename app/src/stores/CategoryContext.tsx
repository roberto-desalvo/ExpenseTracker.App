import React, { createContext, ReactNode, useContext, useEffect, useRef, useState } from "react";
import { useLocation } from "react-router-dom";
import Category from "../models/Category";
import CategoryService from "../services/CategoryService";

interface CategoriesContextType {
  categories: Category[];
  allCategories: Category[];
  isLoading: boolean;
  page: number;
  pageSize: number;
  totalCount: number;
  modifyPage: (newPage: number) => void;
  modifyPageSize: (newPageSize: number) => void;
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
  const location = useLocation();
  const shouldLoadCategories =
    location.pathname === "/categorie" ||
    location.pathname === "/transazioni" ||
    location.pathname === "/";
  const [categories, setCategories] = useState<Category[]>([]);
  const [allCategories, setAllCategories] = useState<Category[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [page, setPage] = useState<number>(0);
  const [pageSize, setPageSize] = useState<number>(10);
  const [totalCount, setTotalCount] = useState<number>(0);
  const currentNameRef = useRef<string | undefined>(undefined);

  const refreshCategories = async (name?: string) => {
    currentNameRef.current = name;
    setIsLoading(true);
    try {
      const result = await CategoryService.getAll({ name, page, pageSize });
      setCategories(result.items);
      setTotalCount(result.totalCount);
    } catch (error) {
      console.error("Errore nel caricamento delle categorie:", error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  const refreshAllCategories = async () => {
    const result = await CategoryService.getAll({ page: 0, pageSize: 10000 });
    setAllCategories(result.items);
  };

  const modifyPage = (newPage: number) => {
    setPage(newPage);
  };

  const modifyPageSize = (newPageSize: number) => {
    setPageSize(newPageSize);
    setPage(0);
  };

  const isMountedRef = useRef(false);

  useEffect(() => {
    if (!shouldLoadCategories) {
      return;
    }

    if (!isMountedRef.current) {
      // Primo mount: carica tutto in parallelo
      isMountedRef.current = true;
      const load = async () => {
        await Promise.all([refreshCategories(), refreshAllCategories()]);
      };
      void load();
    } else {
      // page o pageSize cambiati dopo il mount
      void refreshCategories(currentNameRef.current);
    }
  }, [page, pageSize, shouldLoadCategories]);

  const addCategory = async (category: Category) => {
    try {
      await CategoryService.add(category);
      setPage(0);
      await Promise.all([
        refreshCategories(currentNameRef.current),
        refreshAllCategories(),
      ]);
    } catch (error) {
      console.error("Errore nell'aggiunta della categoria:", error);
      throw error;
    }
  };

  const updateCategory = async (category: Category) => {
    try {
      await CategoryService.update(category);
      await Promise.all([
        refreshCategories(currentNameRef.current),
        refreshAllCategories(),
      ]);
    } catch (error) {
      console.error("Errore nella modifica della categoria:", error);
      throw error;
    }
  };

  const deleteCategory = async (id: number) => {
    try {
      await CategoryService.delete(id);
      await Promise.all([
        refreshCategories(currentNameRef.current),
        refreshAllCategories(),
      ]);
    } catch (error) {
      console.error("Errore nell'eliminazione della categoria:", error);
      throw error;
    }
  };

  return (
    <CategoriesContext.Provider
      value={{
        categories,
        allCategories,
        isLoading,
        page,
        pageSize,
        totalCount,
        modifyPage,
        modifyPageSize,
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
