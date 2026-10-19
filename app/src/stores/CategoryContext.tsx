import React, { createContext, ReactNode, useCallback, useContext, useEffect, useRef, useState } from "react";
import { useLocation } from "react-router-dom";
import { DATA_ROUTES } from "../config/routes";
import Category from "../models/Category";
import CategoryService from "../services/CategoryService";

interface CategoriesContextType {
  categories: Category[];
  allCategories: Category[];
  /** true dopo il primo caricamento (anche fallito) dell'elenco completo */
  categoriesLoaded: boolean;
  isLoading: boolean;
  page: number;
  pageSize: number;
  totalCount: number;
  modifyPage: (newPage: number) => void;
  modifyPageSize: (newPageSize: number) => void;
  addCategory: (category: Category) => Promise<void>;
  updateCategory: (category: Category) => Promise<void>;
  deleteCategory: (id: number) => Promise<void>;
  /** applica il filtro per nome alla lista paginata, ripartendo dalla prima pagina */
  refreshCategories: (name?: string) => void;
  /** attiva la lista paginata finché la vista che la mostra è montata; restituisce la funzione di disattivazione */
  activatePagedList: () => () => void;
}

const CategoriesContext = createContext<CategoriesContextType | undefined>(
  undefined
);

export const CategoriesProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const location = useLocation();
  const shouldLoadCategories =
    DATA_ROUTES.includes(location.pathname);
  const [categories, setCategories] = useState<Category[]>([]);
  const [allCategories, setAllCategories] = useState<Category[]>([]);
  const [categoriesLoaded, setCategoriesLoaded] = useState<boolean>(false);
  const [pagedListActive, setPagedListActive] = useState<boolean>(false);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [page, setPage] = useState<number>(0);
  const [pageSize, setPageSize] = useState<number>(10);
  const [totalCount, setTotalCount] = useState<number>(0);
  const currentNameRef = useRef<string | undefined>(undefined);

  const loadPagedCategories = async () => {
    setIsLoading(true);
    try {
      const result = await CategoryService.getAll({
        name: currentNameRef.current,
        page,
        pageSize,
      });
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
    try {
      const result = await CategoryService.getAll({ page: 0, pageSize: 10000 });
      setAllCategories(result.items);
    } finally {
      setCategoriesLoaded(true);
    }
  };

  /** torna alla prima pagina (l'effect ricarica) oppure, se già lì, ricarica direttamente */
  const reloadFromFirstPage = async () => {
    if (page !== 0) {
      setPage(0);
      return;
    }
    await loadPagedCategories();
  };

  const modifyPage = (newPage: number) => {
    setPage(newPage);
  };

  const modifyPageSize = (newPageSize: number) => {
    setPageSize(newPageSize);
    setPage(0);
  };

  // elenco completo: una sola volta, condiviso da tutte le pagine
  useEffect(() => {
    if (!shouldLoadCategories || categoriesLoaded) {
      return;
    }

    refreshAllCategories().catch(() => {
      // gestito dall'error layer
    });
  }, [shouldLoadCategories]);

  // lista paginata: solo mentre la vista Categorie è montata, e a ogni cambio pagina
  useEffect(() => {
    if (!pagedListActive) {
      return;
    }

    loadPagedCategories().catch(() => {
      // gestito dall'error layer
    });
  }, [pagedListActive, page, pageSize]);

  const activatePagedList = useCallback(() => {
    setPagedListActive(true);
    return () => {
      setPagedListActive(false);
      currentNameRef.current = undefined;
      setPage(0);
    };
  }, []);

  const refreshCategories = (name?: string) => {
    currentNameRef.current = name;
    reloadFromFirstPage().catch(() => {
      // gestito dall'error layer
    });
  };

  const addCategory = async (category: Category) => {
    try {
      await CategoryService.add(category);
      await Promise.all([reloadFromFirstPage(), refreshAllCategories()]);
    } catch (error) {
      console.error("Errore nell'aggiunta della categoria:", error);
      throw error;
    }
  };

  const updateCategory = async (category: Category) => {
    try {
      await CategoryService.update(category);
      await Promise.all([loadPagedCategories(), refreshAllCategories()]);
    } catch (error) {
      console.error("Errore nella modifica della categoria:", error);
      throw error;
    }
  };

  const deleteCategory = async (id: number) => {
    try {
      await CategoryService.delete(id);
      await Promise.all([loadPagedCategories(), refreshAllCategories()]);
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
        categoriesLoaded,
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
        activatePagedList,
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
