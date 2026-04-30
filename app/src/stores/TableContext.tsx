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
import { TransactionMonthOption } from "../models/TransactionMonthOption";

export default interface TableColumn {
  id: "date" | "description" | "amount" | "category" | "account" | "actions";
  label: string;
  minWidth?: string;
  align?: "right";
  format?: (value: number) => string;
}

export type MovementType = "all" | "incomes" | "outcomes";

interface TableContextType {
  columns: TableColumn[];
  selectedAccountIds: number[];
  selectedCategoryIds: number[];
  availableMonths: TransactionMonthOption[];
  selectedMonth: TransactionMonthOption | null;
  movementType: MovementType;
  availableMonthsLoading: boolean;
  page: number;
  pageSize: number;
  modifySelectedMonth: (month: TransactionMonthOption) => void;
  modifyMovementType: (movementType: MovementType) => void;
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
  const [selectedMonth, setSelectedMonth] = useState<TransactionMonthOption | null>(null);
  const [movementType, setMovementType] = useState<MovementType>("all");
  const [page, setPage] = useState<number>(0);
  const [pageSize, setPageSize] = useState<number>(25);
  const transactionContext = useTransactions();

  const buildRequest = (
    month: TransactionMonthOption,
    p: number,
    ps: number,
    accountIds: number[],
    categoryIds: number[],
    selectedMovementType: MovementType,
  ) => {
    const normalizedAccountIds =
      accountIds.length === 0 || accountIds.length === accounts.length
        ? null
        : accountIds;
    const normalizedCategoryIds =
      categoryIds.length === 0 || categoryIds.length === categories.length
        ? null
        : categoryIds;

    return {
      fromDate: month.startDate,
      toDate: month.endDate,
      idAccounts: normalizedAccountIds,
      idCategories: normalizedCategoryIds,
      isIncome:
        selectedMovementType === "all"
          ? null
          : selectedMovementType === "incomes",
      page: p,
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
    if (transactionContext.availableMonths.length === 0) {
      setSelectedMonth(null);
      return;
    }

    setSelectedMonth((currentSelectedMonth) => {
      if (
        currentSelectedMonth &&
        transactionContext.availableMonths.some(
          (month) => month.startDate === currentSelectedMonth.startDate,
        )
      ) {
        return currentSelectedMonth;
      }

      return transactionContext.availableMonths[0];
    });
  }, [transactionContext.availableMonths]);

  useEffect(() => {
    if (!selectedMonth) {
      return;
    }

    transactionContext.refreshTransactions(
      buildRequest(
        selectedMonth,
        page,
        pageSize,
        selectedAccountIds,
        selectedCategoryIds,
        movementType,
      ),
    );
  }, [
    selectedMonth,
    page,
    pageSize,
    selectedAccountIds,
    selectedCategoryIds,
    movementType,
    accounts,
    categories,
  ]);

  const modifySelectedMonth = (month: TransactionMonthOption) => {
    setSelectedMonth(month);
    setPage(0);
  };

  const modifyPage = (newPage: number) => {
    setPage(newPage);
  };

  const modifyPageSize = (newPageSize: number) => {
    setPage(0);
    setPageSize(newPageSize);
  };

  const modifyMovementType = (newMovementType: MovementType) => {
    setMovementType((currentMovementType) =>
      currentMovementType === newMovementType ? "all" : newMovementType,
    );
    setPage(0);
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
        availableMonths: transactionContext.availableMonths,
        selectedMonth,
        movementType,
        availableMonthsLoading: transactionContext.availableMonthsLoading,
        page,
        pageSize,
        modifySelectedMonth,
        modifyMovementType,
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
