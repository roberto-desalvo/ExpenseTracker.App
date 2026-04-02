import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  ReactNode,
} from "react";
import Account from "../models/Account";
import AccountService from "../services/AccountService";

// Definisci il tipo per il valore del contesto
interface AccountsContextType {
  accounts: Account[];
  addAccount: (account: Account) => void;
  updateAccount: (id: number, updatedAccount: Partial<Account>) => void;
  deleteAccount: (id: number) => void;
}

// Crea il Context con un valore iniziale vuoto (sarà fornito dal Provider)
const AccountsContext = createContext<AccountsContextType | undefined>(
  undefined
);

// Definisci il Provider
export const AccountsProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [accounts, setAccounts] = useState<Account[]>([]);

  useEffect(() => {
    const fetchAccounts = async () => {
      try {
        const accounts = await AccountService.getAll();
        setAccounts(accounts);
      } catch (error) {
        console.error("Errore nel caricamento degli accounts:", error);
      }
    };

    fetchAccounts();
  }, []);

  // Funzioni per gestire lo stato
  const addAccount = (account: Account) => {
    setAccounts((prev) => [...prev, account]);
  };

  const updateAccount = (id: number, updatedAccount: Partial<Account>) => {
    setAccounts((prev) =>
      prev.map((account) =>
        account.id === id ? { ...account, ...updatedAccount } : account
      )
    );
  };

  const deleteAccount = (id: number) => {
    setAccounts((prev) => prev.filter((account) => account.id !== id));
  };

  return (
    <AccountsContext.Provider
      value={{ accounts, addAccount, updateAccount, deleteAccount }}
    >
      {children}
    </AccountsContext.Provider>
  );
};

// Hook per utilizzare il contesto in modo più semplice
export const useAccounts = (): AccountsContextType => {
  const context = useContext(AccountsContext);
  if (!context) {
    throw new Error(
      "useAccounts deve essere utilizzato all’interno di AccountsProvider"
    );
  }
  return context;
};
