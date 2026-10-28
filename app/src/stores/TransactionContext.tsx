import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  type ReactNode,
} from "react";
import { useLocation } from "react-router-dom";
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
  availableMonths: TransactionMonthOption[];
  availableMonthsLoading: boolean;
  /** incrementato a ogni modifica (crea/modifica/elimina/trasferimento): le viste aggregate lo osservano per ricaricarsi */
  dataVersion: number;
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
  const location = useLocation();
  const shouldLoadTransactionsData = location.pathname === "/movimenti";
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [availableMonths, setAvailableMonths] = useState<TransactionMonthOption[]>([]);
  const [availableMonthsLoading, setAvailableMonthsLoading] = useState<boolean>(true);
  const [lastRequest, setLastRequest] = useState<TransactionQueryRequest | null>(null);
  const [dataVersion, setDataVersion] = useState<number>(0);
  const bumpDataVersion = () => setDataVersion((v) => v + 1);

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
      } catch (error) {
        console.error("Errore nel caricamento delle transactions:", error);
      } finally {
        setIsLoading(false);
      }
    };

    void fetchTransactions();
  };

  useEffect(() => {
    if (!shouldLoadTransactionsData) {
      return;
    }

    void loadAvailableMonths();
  }, [shouldLoadTransactionsData]);

  /**
   * Dopo una modifica: le viste aggregate si ricaricano tramite `dataVersion`;
   * mesi e lista dei movimenti solo se si è su /movimenti (altrimenti li ricarica l'atterraggio sulla pagina).
   */
  const reloadAfterMutation = async () => {
    bumpDataVersion();

    if (!shouldLoadTransactionsData) {
      return;
    }

    await loadAvailableMonths();
    refreshTransactions();
  };

  const addTransaction = async (transaction: Transaction) => {
    try {
      await TransactionService.add(transaction);
      await reloadAfterMutation();
    } catch (error) {
      console.error("Errore nell'aggiunta della transaction:", error);
      throw error;
    }
  };

  const updateTransaction = async (transaction: Transaction) => {
    try {
      await TransactionService.update(transaction);
      await reloadAfterMutation();
    } catch (error) {
      console.error("Errore nella modifica della transaction:", error);
      throw error;
    }
  };

  const addTransfer = async (transfer: TransferPayload) => {
    try {
      await TransferService.add(transfer);
      await reloadAfterMutation();
    } catch (error) {
      console.error("Errore nell'aggiunta del trasferimento:", error);
      throw error;
    }
  };

  const deleteTransaction = (transaction: Transaction) => {
    const removeTransaction = async () => {
      try {
        await TransactionService.delete(transaction.id);
        await reloadAfterMutation();
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
        availableMonths,
        availableMonthsLoading,
        dataVersion,
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
