import React, {
  createContext,
  useContext,
  useState,
  ReactNode,
  useEffect,
} from "react";
import Account from "../models/Account";
import { useAccounts } from "./AccountContext";
import { useTransactions } from "./TransactionContext";
import Transaction from "../models/Transaction";

export default interface TableColumn {
  id: "date" | "description" | "amount" | "category" | "account" | "edit" | "delete";
  label: string;
  minWidth?: string;
  align?: "right";
  format?: (value: number) => string;
}

interface TableContextType {
  columns: TableColumn[];
  selectedAccounts: Account[];
  filterDate: Date;
  includeMoneyTransfers: boolean;
  toggleIncludeMoneyTransfers: () => void;
  modifyFilterDate: (date: Date) => void;
  addToSelectedAccount: (account: Account) => void;
  removeFromSelectedAccount: (account: Account) => void;
  getFilteredTransactions: () => Transaction[];
}

const TableContext = createContext<TableContextType | undefined>(undefined);

export const TableContextProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [columns] = useState<TableColumn[]>([
    {
      id: "date",
      label: "Date",
      minWidth: "15%",
    },
    { id: "description", label: "Description", minWidth: "35%" },
    {
      id: "amount",
      label: "Amount",
      minWidth: "10%",
      format: (value: number) => value.toLocaleString("it-IT"),
    },
    {
      id: "category",
      label: "Category",
      minWidth: "20%",
      format: (value: number) => value.toLocaleString("it-IT"),
    },
    {
      id: "account",
      label: "Account",
      minWidth: "10%",
      format: (value: number) => value.toFixed(2),
    },
    {
      id: "edit",
      label: "",
      minWidth: "5%"
    },
    {
      id: "delete",
      label: "",
      minWidth: "5%"
    }
  ]);

  const { accounts } = useAccounts();
  const [selectedAccounts, setSelectedAccounts] = useState<Account[]>([]);
  const [filterDate, setFilterDate] = useState<Date>(new Date());
  const [includeMoneyTransfers, setIncludeMoneyTransfers] =
    useState<boolean>(true);
  const transactionContext = useTransactions();

  useEffect(() => {
    if (accounts && accounts.length > 0) {
      setSelectedAccounts(accounts);
    }
  }, [accounts]);

  const toggleIncludeMoneyTransfers = () => {
    const newValue = !includeMoneyTransfers;
    setIncludeMoneyTransfers(newValue);
  };

  const modifySelectedMonthAndYear = (date: Date) => {
    setFilterDate(date);
  };

  const addToSelectedAccount = (account: Account) => {
    if (!selectedAccounts.find((a) => a.id === account.id)) {
      setSelectedAccounts((prev) => [...prev, account]);
    }
  };

  const removeFromSelectedAccount = (account: Account) => {
    setSelectedAccounts((prev) => prev.filter((a) => a.id !== account.id));
  };

  const getFilteredTransactions = () => {
    const transactions = transactionContext.transactions;
    return transactions
      .filter((transaction) => {
        const transactionDate = new Date(transaction.date);
        return (
          transactionDate.getFullYear() === filterDate.getFullYear() &&
          transactionDate.getMonth() === filterDate.getMonth()
        );
      })
      .filter((transaction) => {
        return (
          selectedAccounts
            .map((a) => a.id)
            .filter((id) => id == transaction.accountId).length > 0
        );
      })
      .filter((transaction) => {
        return includeMoneyTransfers
          ? true
          : transaction.category.toLowerCase() !==
              "Money Transfers".toLowerCase();
      })
      .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
  };

  return (
    <TableContext.Provider
      value={{
        columns,
        selectedAccounts,
        filterDate,
        includeMoneyTransfers,
        toggleIncludeMoneyTransfers,
        modifyFilterDate: modifySelectedMonthAndYear,
        addToSelectedAccount,
        removeFromSelectedAccount,
        getFilteredTransactions,
      }}
    >
      {children}
    </TableContext.Provider>
  );
};

export const useTableContext = (): TableContextType => {
  const context = useContext(TableContext);
  if (!context) {
    throw new Error(
      "useTableContext deve essere utilizzato all’interno di TableContextProvider"
    );
  }
  return context;
};
