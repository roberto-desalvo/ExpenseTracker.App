import React, {
  createContext,
  useContext,
  useState,
  ReactNode,
  useEffect,
} from "react";
import { useAccounts } from "./AccountContext";
import { useCategories } from "./CategoryContext";
import { useTransactions } from "./TransactionContext";
import Transaction from "../models/Transaction";

export default interface TableColumn {
  id: "date" | "description" | "amount" | "category" | "account" | "actions";
  label: string;
  minWidth?: string;
  align?: "right";
  format?: (value: number) => string;
}

interface TableContextType {
  columns: TableColumn[];
  selectedAccountIds: number[];
  selectedCategoryIds: number[];
  filterDate: Date;
  page: number;
  pageSize: number;
  includeMoneyTransfers: boolean;
  toggleIncludeMoneyTransfers: () => void;
  modifyFilterDate: (date: Date) => void;
  modifyPage: (page: number) => void;
  modifyPageSize: (pageSize: number) => void;
  modifySelectedAccountIds: (accountIds: number[]) => void;
  modifySelectedCategoryIds: (categoryIds: number[]) => void;
  getFilteredTransactions: () => Transaction[];
}

const TableContext = createContext<TableContextType | undefined>(undefined);

export const TableContextProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [columns] = useState<TableColumn[]>([
    {
      id: "date",
      label: "Data",
      minWidth: "15%",
    },
    { id: "description", label: "Descrizione", minWidth: "35%" },
    {
      id: "amount",
      label: "Importo",
      minWidth: "10%",
      format: (value: number) => value.toLocaleString("it-IT"),
    },
    {
      id: "category",
      label: "Categoria",
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
      id: "actions",
      label: "Azioni",
      minWidth: "10%",
    },
  ]);

  const { accounts } = useAccounts();
  const { categories } = useCategories();
  const [selectedAccountIds, setSelectedAccountIds] = useState<number[]>([]);
  const [selectedCategoryIds, setSelectedCategoryIds] = useState<number[]>([]);
  const [filterDate, setFilterDate] = useState<Date>(new Date());
  const [page, setPage] = useState<number>(0);
  const [pageSize, setPageSize] = useState<number>(25);
  const [includeMoneyTransfers, setIncludeMoneyTransfers] =
    useState<boolean>(true);
  const transactionContext = useTransactions();

  const buildRequest = (
    date: Date,
    p: number,
    ps: number,
    accountIds: number[],
    categoryIds: number[],
    includeTransfers: boolean,
  ) => {
    const from = new Date(date.getFullYear(), date.getMonth(), 1);
    const to = new Date(date.getFullYear(), date.getMonth() + 1, 0, 23, 59, 59);
    const normalizedAccountIds =
      accountIds.length === 0 || accountIds.length === accounts.length
        ? null
        : accountIds;
    const normalizedCategoryIds =
      categoryIds.length === 0 || categoryIds.length === categories.length
        ? null
        : categoryIds;

    return {
      fromDate: from.toISOString(),
      toDate: to.toISOString(),
      idAccounts: normalizedAccountIds,
      idCategories: normalizedCategoryIds,
      includeMoneyTransfers: includeTransfers,
      page: p + 1,
      pageSize: ps,
    };
  };

  useEffect(() => {
    setSelectedAccountIds((prev) =>
      prev.filter((selectedId) =>
        accounts.some((account) => account.id === selectedId),
      ),
    );
  }, [accounts]);

  useEffect(() => {
    setSelectedCategoryIds((prev) =>
      prev.filter((selectedId) =>
        categories.some((category) => category.id === selectedId),
      ),
    );
  }, [categories]);

  useEffect(() => {
    transactionContext.refreshTransactions(
      buildRequest(
        filterDate,
        page,
        pageSize,
        selectedAccountIds,
        selectedCategoryIds,
        includeMoneyTransfers,
      ),
    );
  }, [
    filterDate,
    page,
    pageSize,
    selectedAccountIds,
    selectedCategoryIds,
    includeMoneyTransfers,
    accounts,
    categories,
  ]);

  const toggleIncludeMoneyTransfers = () => {
    const newValue = !includeMoneyTransfers;
    setIncludeMoneyTransfers(newValue);
    setPage(0);
  };

  const modifySelectedMonthAndYear = (date: Date) => {
    setFilterDate(date);
    setPage(0);
  };

  const modifyPage = (newPage: number) => {
    setPage(newPage);
  };

  const modifyPageSize = (newPageSize: number) => {
    setPage(0);
    setPageSize(newPageSize);
  };

  const modifySelectedAccountIds = (accountIds: number[]) => {
    setSelectedAccountIds(accountIds);
    setPage(0);
  };

  const modifySelectedCategoryIds = (categoryIds: number[]) => {
    setSelectedCategoryIds(categoryIds);
    setPage(0);
  };

  const getFilteredTransactions = () => {
    const transactions = transactionContext.transactions;
    return transactions.sort(
      (a, b) => new Date(b.date).getTime() - new Date(a.date).getTime(),
    );
  };

  return (
    <TableContext.Provider
      value={{
        columns,
        selectedAccountIds,
        selectedCategoryIds,
        filterDate,
        page,
        pageSize,
        includeMoneyTransfers,
        toggleIncludeMoneyTransfers,
        modifyFilterDate: modifySelectedMonthAndYear,
        modifyPage,
        modifyPageSize,
        modifySelectedAccountIds,
        modifySelectedCategoryIds,
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
      "useTableContext deve essere utilizzato all’interno di TableContextProvider",
    );
  }
  return context;
};
