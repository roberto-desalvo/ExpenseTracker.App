import React, {
  createContext,
  useContext,
  useState,
  ReactNode,
  useEffect,
} from "react";
import { useLocation } from "react-router-dom";
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
  const location = useLocation();
  const shouldLoadTableData = location.pathname === "/movimenti";
  const [columns] = useState<TableColumn[]>([
    { id: "date", label: "Data", minWidth: "12%" },
    { id: "description", label: "Descrizione", minWidth: "34%" },
    { id: "category", label: "Categoria", minWidth: "18%" },
    { id: "account", label: "Conto", minWidth: "14%" },
    { id: "amount", label: "Importo", minWidth: "14%", align: "right" },
    { id: "actions", label: "", minWidth: "8%", align: "right" },
  ]);

  const { accounts } = useAccounts();
  const { allCategories: categories } = useCategories();
  const [selectedAccountIds, setSelectedAccountIds] = useState<number[]>([]);
  const [selectedCategoryIds, setSelectedCategoryIds] = useState<number[]>([]);
  const [selectedMonth, setSelectedMonth] = useState<TransactionMonthOption | null>(null);
  const [movementType, setMovementType] = useState<MovementType>("all");
  const [page, setPage] = useState<number>(0);
  const [pageSize, setPageSize] = useState<number>(10);
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

  // rimuove dalla selezione gli id non più esistenti; se non cambia nulla mantiene lo stesso array
  const keepExisting = (prev: number[], existingIds: number[]) => {
    const next = prev.filter((selectedId) => existingIds.includes(selectedId));
    return next.length === prev.length ? prev : next;
  };

  useEffect(() => {
    setSelectedAccountIds((prev) =>
      keepExisting(prev, accounts.map((account) => account.id)),
    );
  }, [accounts]);

  useEffect(() => {
    setSelectedCategoryIds((prev) =>
      keepExisting(prev, categories.map((category) => category.id)),
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

  const request = selectedMonth
    ? buildRequest(
        selectedMonth,
        page,
        pageSize,
        selectedAccountIds,
        selectedCategoryIds,
        movementType,
      )
    : null;
  // la richiesta parte solo quando cambia davvero (filtri, pagina, mese), non a ogni nuovo riferimento di conti/categorie
  const requestKey = request ? JSON.stringify(request) : null;

  useEffect(() => {
    if (!shouldLoadTableData || !request) {
      return;
    }

    transactionContext.refreshTransactions(request);
  }, [shouldLoadTableData, requestKey]);

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
