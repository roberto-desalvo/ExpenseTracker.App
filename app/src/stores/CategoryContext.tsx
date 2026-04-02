import React, {
    createContext,
    useContext,
    useState,
    useEffect,
    ReactNode,
  } from "react";
  import Category from "../models/Category";
  import CategoryService from "../services/CategoryService";
  
  interface CategoriesContextType {
    categories: Category[];
    addCategory: (category: Category) => void;
    updateCategory: (id: number, updatedCategory: Partial<Category>) => void;
    deleteCategory: (id: number) => void;
  }
  
  // Crea il Context con un valore iniziale vuoto (sarà fornito dal Provider)
  const CategoriesContext = createContext<CategoriesContextType | undefined>(
    undefined
  );
  
  // Definisci il Provider
  export const CategoriesProvider: React.FC<{ children: ReactNode }> = ({
    children,
  }) => {
    const [categories, setCategories] = useState<Category[]>([]);
  
    useEffect(() => {
      const fetchCategories = async () => {
        try {
          const categories = await CategoryService.getAll();
          setCategories(categories);
        } catch (error) {
          console.error("Errore nel caricamento delle categorys:", error);
        }
      };
  
      fetchCategories();
    }, []);
  
    // Funzioni per gestire lo stato
    const addCategory = (category: Category) => {
      setCategories((prev) => [...prev, category]);
    };
  
    const updateCategory = (id: number, updatedCategory: Partial<Category>) => {
      setCategories((prev) =>
        prev.map((category) =>
          category.id === id ? { ...category, ...updatedCategory } : category
        )
      );
    };
  
    const deleteCategory = (id: number) => {
      setCategories((prev) => prev.filter((category) => category.id !== id));
    };
  
    return (
      <CategoriesContext.Provider
        value={{ categories: categories, addCategory, updateCategory, deleteCategory }}
      >
        {children}
      </CategoriesContext.Provider>
    );
  };
  
  // Hook per utilizzare il contesto in modo più semplice
  export const useCategories = (): CategoriesContextType => {
    const context = useContext(CategoriesContext);
    if (!context) {
      throw new Error(
        "useCategories deve essere utilizzato all’interno di CategoriesProvider"
      );
    }
    return context;
  };
  