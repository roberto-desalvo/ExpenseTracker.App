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
import { SortDirection, TransactionSortField } from "../models/TransactionQueryRequest";

export default interface TableColumn {
  id: TransactionSortField | "actions";
  label: string;
  minWidth?: string;
  align?: "right";
  sortable?: boolean;
  format?: (value: number) => string;
}

// date e importo partono dal più recente/più alto, i campi testuali in ordine alfabetico
const defaultSortDirection = (field: TransactionSortField): SortDirection =>
  field === "date" || field === "amount" ? "desc" : "asc";

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
  sortBy: TransactionSortField;
  sortDirection: SortDirection;
  modifySort: (field: TransactionSortField) => void;
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
    { id: "date", label: "Data", minWidth: "12%", sortable: true },
    { id: "description", label: "Descrizione", minWidth: "34%", sortable: true },
    { id: "category", label: "Categoria", minWidth: "18%", sortable: true },
    { id: "account", label: "Conto", minWidth: "14%", sortable: true },
    { id: "amount", label: "Importo", minWidth: "14%", align: "right", sortable: true },
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
  const [sortBy, setSortBy] = useState<TransactionSortField>("date");
  const [sortDirection, setSortDirection] = useState<SortDirection>("desc");
  const transactionContext = useTransactions();

  const buildRequest = (
    month: TransactionMonthOption,
    p: number,
    ps: number,
    accountIds: number[],
    categoryIds: number[],
    selectedMovementType: MovementType,
    field: TransactionSortField,
    direction: SortDirection,
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
      sortBy: field,
      sortDirection: direction,
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
        sortBy,
        sortDirection,
      )
    : null;
  // la richiesta parte solo quando cambia davvero (filtri, ordinamento, pagina, mese), non a ogni nuovo riferimento di conti/categorie
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

  // stessa colonna: inverte la direzione; nuova colonna: parte dalla sua direzione naturale
  const modifySort = (field: TransactionSortField) => {
    if (field === sortBy) {
      setSortDirection((current) => (current === "asc" ? "desc" : "asc"));
    } else {
      setSortBy(field);
      setSortDirection(defaultSortDirection(field));
    }
    setPage(0);
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

  // l'ordinamento è applicato lato server (insieme alla paginazione)
  const getFilteredTransactions = () => transactionContext.transactions;

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
        sortBy,
        sortDirection,
        modifySort,
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
