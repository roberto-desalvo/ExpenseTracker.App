import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  useRef,
  ReactNode,
} from "react";
import { useLocation } from "react-router-dom";
import Account from "../models/Account";
import AccountService from "../services/AccountService";

interface AccountsContextType {
  accounts: Account[];
  pagedAccounts: Account[];
  isLoading: boolean;
  page: number;
  pageSize: number;
  totalCount: number;
  modifyPage: (newPage: number) => void;
  modifyPageSize: (newPageSize: number) => void;
  refreshAccounts: (name?: string) => Promise<void>;
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
    location.pathname === "/impostazioni" ||
    location.pathname === "/";
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [pagedAccounts, setPagedAccounts] = useState<Account[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [page, setPage] = useState<number>(0);
  const [pageSize, setPageSize] = useState<number>(10);
  const [totalCount, setTotalCount] = useState<number>(0);
  const currentNameRef = useRef<string | undefined>(undefined);

  const refreshAllAccounts = async () => {
    const result = await AccountService.query({ page: 0, pageSize: 10000 });
    setAccounts(result.items);
  };

  const refreshAccounts = async (name?: string) => {
    currentNameRef.current = name;
    setIsLoading(true);
    try {
      const result = await AccountService.query({ name, page, pageSize });
      setPagedAccounts(result.items);
      setTotalCount(result.totalCount);
    } catch (error) {
      console.error("Errore nel caricamento degli account:", error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  const isMountedRef = useRef(false);

  useEffect(() => {
    if (!shouldLoadAccounts) {
      return;
    }

    if (!isMountedRef.current) {
      // Primo mount: carica tutto in parallelo
      isMountedRef.current = true;
      const load = async () => {
        try {
          await Promise.all([refreshAllAccounts(), refreshAccounts()]);
        } catch {
          // gestito dall'error layer
        }
      };
      void load();
    } else {
      // page o pageSize cambiati dopo il mount
      void refreshAccounts(currentNameRef.current);
    }
  }, [page, pageSize, shouldLoadAccounts]);

  const modifyPage = (newPage: number) => {
    setPage(newPage);
  };

  const modifyPageSize = (newPageSize: number) => {
    setPageSize(newPageSize);
    setPage(0);
  };

  const addAccount = async (account: Account) => {
    try {
      await AccountService.add(account);
      setPage(0);
      await Promise.all([
        refreshAllAccounts(),
        refreshAccounts(currentNameRef.current),
      ]);
    } catch (error) {
      console.error("Errore nell'aggiunta dell'account:", error);
      throw error;
    }
  };

  const updateAccount = async (account: Account) => {
    try {
      await AccountService.update(account);
      await Promise.all([
        refreshAllAccounts(),
        refreshAccounts(currentNameRef.current),
      ]);
    } catch (error) {
      console.error("Errore nella modifica dell'account:", error);
      throw error;
    }
  };

  return (
    <AccountsContext.Provider
      value={{
        accounts,
        pagedAccounts,
        isLoading,
        page,
        pageSize,
        totalCount,
        modifyPage,
        modifyPageSize,
        refreshAccounts,
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
