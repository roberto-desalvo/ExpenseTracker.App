import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  useRef,
  useCallback,
  ReactNode,
} from "react";
import { useLocation } from "react-router-dom";
import { DATA_ROUTES } from "../config/routes";
import Account from "../models/Account";
import AccountService from "../services/AccountService";

interface AccountsContextType {
  accounts: Account[];
  /** true dopo il primo caricamento (anche fallito) dell'elenco completo */
  accountsLoaded: boolean;
  pagedAccounts: Account[];
  isLoading: boolean;
  page: number;
  pageSize: number;
  totalCount: number;
  modifyPage: (newPage: number) => void;
  modifyPageSize: (newPageSize: number) => void;
  /** applica il filtro per nome alla lista paginata, ripartendo dalla prima pagina */
  refreshAccounts: (name?: string) => void;
  /** attiva la lista paginata finché la vista che la mostra è montata; restituisce la funzione di disattivazione */
  activatePagedList: () => () => void;
  addAccount: (account: Account) => Promise<void>;
  updateAccount: (account: Account) => Promise<void>;
}

const AccountsContext = createContext<AccountsContextType | undefined>(
  undefined
);

export const AccountsProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const location = useLocation();
  const shouldLoadAccounts =
    DATA_ROUTES.includes(location.pathname);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [accountsLoaded, setAccountsLoaded] = useState<boolean>(false);
  const [pagedAccounts, setPagedAccounts] = useState<Account[]>([]);
  const [pagedListActive, setPagedListActive] = useState<boolean>(false);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [page, setPage] = useState<number>(0);
  const [pageSize, setPageSize] = useState<number>(10);
  const [totalCount, setTotalCount] = useState<number>(0);
  const currentNameRef = useRef<string | undefined>(undefined);

  const refreshAllAccounts = async () => {
    try {
      const result = await AccountService.query({ page: 0, pageSize: 10000 });
      setAccounts(result.items);
    } finally {
      setAccountsLoaded(true);
    }
  };

  const loadPagedAccounts = async () => {
    setIsLoading(true);
    try {
      const result = await AccountService.query({
        name: currentNameRef.current,
        page,
        pageSize,
      });
      setPagedAccounts(result.items);
      setTotalCount(result.totalCount);
    } catch (error) {
      console.error("Errore nel caricamento degli account:", error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  /** torna alla prima pagina (l'effect ricarica) oppure, se già lì, ricarica direttamente */
  const reloadFromFirstPage = async () => {
    if (page !== 0) {
      setPage(0);
      return;
    }
    await loadPagedAccounts();
  };

  // elenco completo: una sola volta, condiviso da tutte le pagine
  useEffect(() => {
    if (!shouldLoadAccounts || accountsLoaded) {
      return;
    }

    refreshAllAccounts().catch(() => {
      // gestito dall'error layer
    });
  }, [shouldLoadAccounts]);

  // lista paginata: solo mentre la vista Conti è montata, e a ogni cambio pagina
  useEffect(() => {
    if (!pagedListActive) {
      return;
    }

    loadPagedAccounts().catch(() => {
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

  const modifyPage = (newPage: number) => {
    setPage(newPage);
  };

  const modifyPageSize = (newPageSize: number) => {
    setPageSize(newPageSize);
    setPage(0);
  };

  const refreshAccounts = (name?: string) => {
    currentNameRef.current = name;
    reloadFromFirstPage().catch(() => {
      // gestito dall'error layer
    });
  };

  const addAccount = async (account: Account) => {
    try {
      await AccountService.add(account);
      await Promise.all([refreshAllAccounts(), reloadFromFirstPage()]);
    } catch (error) {
      console.error("Errore nell'aggiunta dell'account:", error);
      throw error;
    }
  };

  const updateAccount = async (account: Account) => {
    try {
      await AccountService.update(account);
      await Promise.all([refreshAllAccounts(), loadPagedAccounts()]);
    } catch (error) {
      console.error("Errore nella modifica dell'account:", error);
      throw error;
    }
  };

  return (
    <AccountsContext.Provider
      value={{
        accounts,
        accountsLoaded,
        pagedAccounts,
        isLoading,
        page,
        pageSize,
        totalCount,
        modifyPage,
        modifyPageSize,
        refreshAccounts,
        activatePagedList,
        addAccount,
        updateAccount,
      }}
    >
      {children}
    </AccountsContext.Provider>
  );
};

export const useAccounts = (): AccountsContextType => {
  const context = useContext(AccountsContext);
  if (!context) {
    throw new Error(
      "useAccounts deve essere utilizzato all’interno di AccountsProvider"
    );
  }
  return context;
};
