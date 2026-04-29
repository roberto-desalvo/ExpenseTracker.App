import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  ReactNode,
} from "react";
import Transaction from "../models/Transaction";
import TransactionService from "../services/TransactionService";
import { TransactionQueryRequest } from "../models/TransactionQueryRequest";

interface TransactionsContextType {
  transactions: Transaction[];
  totalCount: number;
  addTransaction: (transaction: Transaction) => void;
  updateTransaction: (
    id: number,
    updatedTransaction: Partial<Transaction>
  ) => void;
  deleteTransaction: (transaction: Transaction) => void;
  refreshTransactions: (request?: TransactionQueryRequest) => void;
}

const TransactionsContext = createContext<TransactionsContextType | undefined>(
  undefined
);

export const TransactionsProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [totalCount, setTotalCount] = useState<number>(0);

  const refreshTransactions = (request?: TransactionQueryRequest) => {
    const fetchTransactions = async () => {
      try {
        const req = request ?? { page: 1, pageSize: 25 };
        const result = await TransactionService.getAll(req);
        setTransactions(result.items);
        setTotalCount(result.totalCount);
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
        totalCount,
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
