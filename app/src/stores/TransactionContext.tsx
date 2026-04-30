import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  type ReactNode,
} from "react";
import Transaction from "../models/Transaction";
import { TransferPayload } from "../models/Transfer.ts";
import { TransactionMonthOption } from "../models/TransactionMonthOption";
import TransactionService from "../services/TransactionService";
import TransferService from "../services/TransferService.ts";
import { TransactionQueryRequest } from "../models/TransactionQueryRequest";

interface TransactionsContextType {
  transactions: Transaction[];
  isLoading: boolean;
  totalCount: number;
  totalIncomes: number;
  totalOutcomes: number;
  totalNet: number;
  availableMonths: TransactionMonthOption[];
  availableMonthsLoading: boolean;
  addTransaction: (transaction: Transaction) => Promise<void>;
  addTransfer: (transfer: TransferPayload) => Promise<void>;
  updateTransaction: (transaction: Transaction) => Promise<void>;
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
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [totalIncomes, setTotalIncomes] = useState<number>(0);
  const [totalOutcomes, setTotalOutcomes] = useState<number>(0);
  const [totalNet, setTotalNet] = useState<number>(0);
  const [availableMonths, setAvailableMonths] = useState<TransactionMonthOption[]>([]);
  const [availableMonthsLoading, setAvailableMonthsLoading] = useState<boolean>(true);
  const [lastRequest, setLastRequest] = useState<TransactionQueryRequest | null>(null);

  const loadAvailableMonths = async () => {
    setAvailableMonthsLoading(true);

    try {
      const result = await TransactionService.getMonthOptions();
      setAvailableMonths(result);
      return result;
    } catch (error) {
      console.error("Errore nel caricamento dei mesi disponibili:", error);
      setAvailableMonths([]);
      return [];
    } finally {
      setAvailableMonthsLoading(false);
    }
  };

  const refreshTransactions = (request?: TransactionQueryRequest) => {
    const effectiveRequest = request ?? lastRequest;

    if (!effectiveRequest) {
      return;
    }

    if (request) {
      setLastRequest(request);
    }

    const fetchTransactions = async () => {
      setIsLoading(true);
      try {
        const result = await TransactionService.getAll(effectiveRequest);
        setTransactions(result.items);
        setTotalCount(result.totalCount);
        setTotalIncomes(result.totalIncomes ?? 0);
        setTotalOutcomes(result.totalOutcomes ?? 0);
        setTotalNet(result.totalNet ?? 0);
      } catch (error) {
        console.error("Errore nel caricamento delle transactions:", error);
      } finally {
        setIsLoading(false);
      }
    };

    void fetchTransactions();
  };

  useEffect(() => {
    void loadAvailableMonths();
  }, []);

  const addTransaction = async (transaction: Transaction) => {
    try {
      await TransactionService.add(transaction);
      await loadAvailableMonths();
      refreshTransactions();
    } catch (error) {
      console.error("Errore nell'aggiunta della transaction:", error);
      throw error;
    }
  };

  const updateTransaction = async (transaction: Transaction) => {
    try {
      await TransactionService.update(transaction);
      await loadAvailableMonths();
      refreshTransactions();
    } catch (error) {
      console.error("Errore nella modifica della transaction:", error);
      throw error;
    }
  };

  const addTransfer = async (transfer: TransferPayload) => {
    try {
      await TransferService.add(transfer);
      await loadAvailableMonths();
      refreshTransactions();
    } catch (error) {
      console.error("Errore nell'aggiunta del trasferimento:", error);
      throw error;
    }
  };

  const deleteTransaction = (transaction: Transaction) => {
    const removeTransaction = async () => {
      try {
        await TransactionService.delete(transaction.id);
        await loadAvailableMonths();
        refreshTransactions();
      } catch (error) {
        console.error("Errore nell'eliminazione della transaction:", error);
      }
    };

    void removeTransaction();
  };

  return (
    <TransactionsContext.Provider
      value={{
        transactions,
        isLoading,
        totalCount,
        totalIncomes,
        totalOutcomes,
        totalNet,
        availableMonths,
        availableMonthsLoading,
        addTransaction,
        addTransfer,
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
