import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  ReactNode,
} from "react";
import Transaction from "../models/Transaction";
import TransactionService from "../services/TransactionService";

interface TransactionsContextType {
  transactions: Transaction[];
  addTransaction: (transaction: Transaction) => void;
  updateTransaction: (
    id: number,
    updatedTransaction: Partial<Transaction>
  ) => void;
  deleteTransaction: (transaction: Transaction) => void;
  refreshTransactions: () => void;
}

const TransactionsContext = createContext<TransactionsContextType | undefined>(
  undefined
);

export const TransactionsProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [transactions, setTransactions] = useState<Transaction[]>([]);

  const refreshTransactions = () => {
    const fetchTransactions = async () => {
      try {
        const transactions = await TransactionService.getAll();
        setTransactions(transactions);
      } catch (error) {
        console.error("Errore nel caricamento delle transactions:", error);
      }
    };

    fetchTransactions();
  };

  useEffect(() => {
    refreshTransactions();
  }, []);

  const addTransaction = (transaction: Transaction) => {
    TransactionService.add(transaction);
    refreshTransactions();
  };

  const updateTransaction = (
    id: number,
    updatedTransaction: Partial<Transaction>
  ) => {
    setTransactions((prev) =>
      prev.map((transaction) =>
        transaction.id === id
          ? { ...transaction, ...updatedTransaction }
          : transaction
      )
    );
  };

  const deleteTransaction = (transaction: Transaction) => {
    TransactionService.delete(transaction.id);
    refreshTransactions();
  };

  return (
    <TransactionsContext.Provider
      value={{
        transactions,
        addTransaction,
        updateTransaction,
        deleteTransaction,
        refreshTransactions,
      }}
    >
      {children}
    </TransactionsContext.Provider>
  );
};

export const useTransactions = (): TransactionsContextType => {
  const context = useContext(TransactionsContext);
  if (!context) {
    throw new Error(
      "useTransactions deve essere utilizzato all’interno di TransactionsProvider"
    );
  }
  return context;
};
