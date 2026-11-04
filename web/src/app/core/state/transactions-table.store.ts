import { Injectable, computed, effect, inject, signal, untracked } from "@angular/core";
import { TransactionMonthOption } from "../models/TransactionMonthOption";
import { SortDirection, TransactionQueryRequest, TransactionSortField } from "../models/TransactionQueryRequest";
import { AccountsStore } from "./accounts.store";
import { CategoriesStore } from "./categories.store";
import { TransactionsStore } from "./transactions.store";

export interface TableColumn {
  id: TransactionSortField | "actions";
  label: string;
  minWidth?: string;
  align?: "right";
  sortable?: boolean;
}

export type MovementType = "all" | "incomes" | "outcomes";
export type PeriodMode = "month" | "custom";

/** Intervallo personalizzato, estremi inclusi in formato YYYY-MM-DD; null = nessun limite da quel lato. */
export interface DateRange {
  from: string | null;
  to: string | null;
}

export const TRANSACTION_COLUMNS: TableColumn[] = [
  { id: "date", label: "Data", minWidth: "12%", sortable: true },
  { id: "description", label: "Descrizione", minWidth: "34%", sortable: true },
  { id: "category", label: "Categoria", minWidth: "18%", sortable: true },
  { id: "account", label: "Conto", minWidth: "14%", sortable: true },
  { id: "amount", label: "Importo", minWidth: "14%", align: "right", sortable: true },
  { id: "actions", label: "", minWidth: "8%", align: "right" },
];

// date e importo partono dal più recente/più alto, i campi testuali in ordine alfabetico
const defaultSortDirection = (field: TransactionSortField): SortDirection =>
  field === "date" || field === "amount" ? "desc" : "asc";

// rimuove dalla selezione gli id non più esistenti; se non cambia nulla mantiene lo stesso array
const keepExisting = (prev: number[], existingIds: number[]) => {
  const next = prev.filter((selectedId) => existingIds.includes(selectedId));
  return next.length === prev.length ? prev : next;
};

/**
 * Filtri, ordinamento e paginazione della tabella Movimenti.
 * Mentre la pagina è attiva, ogni variazione effettiva della richiesta ricarica la lista.
 */
@Injectable({ providedIn: "root" })
export class TransactionsTableStore {
  private readonly accountsStore = inject(AccountsStore);
  private readonly categoriesStore = inject(CategoriesStore);
  private readonly transactionsStore = inject(TransactionsStore);

  readonly columns = TRANSACTION_COLUMNS;
  readonly selectedAccountIds = signal<number[]>([]);
  readonly selectedCategoryIds = signal<number[]>([]);
  readonly selectedMonth = signal<TransactionMonthOption | null>(null);
  readonly periodMode = signal<PeriodMode>("month");
  readonly customRange = signal<DateRange>({ from: null, to: null });
  readonly movementType = signal<MovementType>("all");
  readonly page = signal(0);
  readonly pageSize = signal(10);
  readonly sortBy = signal<TransactionSortField>("date");
  readonly sortDirection = signal<SortDirection>("desc");

  readonly availableMonths = this.transactionsStore.availableMonths;
  readonly availableMonthsLoading = this.transactionsStore.availableMonthsLoading;

  private readonly active = signal(false);

  readonly request = computed<TransactionQueryRequest | null>(() => {
    // mese: dal primo all'ultimo giorno; personalizzato: giorni interi, estremi inclusi
    const month = this.selectedMonth();
    const range = this.customRange();
    const period =
      this.periodMode() === "month"
        ? month && { fromDate: month.startDate, toDate: month.endDate }
        : {
            fromDate: range.from ? `${range.from}T00:00:00` : undefined,
            toDate: range.to ? `${range.to}T23:59:59.999` : undefined,
          };

    if (!period) return null;

    const accountIds = this.selectedAccountIds();
    const categoryIds = this.selectedCategoryIds();
    const accountsCount = this.accountsStore.all().length;
    const categoriesCount = this.categoriesStore.all().length;
    const movementType = this.movementType();

    return {
      fromDate: period.fromDate,
      toDate: period.toDate,
      idAccounts: accountIds.length === 0 || accountIds.length === accountsCount ? null : accountIds,
      idCategories: categoryIds.length === 0 || categoryIds.length === categoriesCount ? null : categoryIds,
      isIncome: movementType === "all" ? null : movementType === "incomes",
      sortBy: this.sortBy(),
      sortDirection: this.sortDirection(),
      page: this.page(),
      pageSize: this.pageSize(),
    };
  });

  // la richiesta parte solo quando cambia davvero (filtri, ordinamento, pagina, periodo), non a ogni nuovo riferimento di conti/categorie
  private readonly requestKey = computed(() => {
    const request = this.request();
    return request ? JSON.stringify(request) : null;
  });

  constructor() {
    effect(() => {
      const ids = this.accountsStore.all().map((account) => account.id);
      untracked(() => this.selectedAccountIds.update((prev) => keepExisting(prev, ids)));
    });

    effect(() => {
      const ids = this.categoriesStore.all().map((category) => category.id);
      untracked(() => this.selectedCategoryIds.update((prev) => keepExisting(prev, ids)));
    });

    effect(() => {
      const months = this.availableMonths();
      untracked(() => {
        if (months.length === 0) {
          this.selectedMonth.set(null);
          return;
        }
        const current = this.selectedMonth();
        if (!current || !months.some((month) => month.startDate === current.startDate)) {
          this.selectedMonth.set(months[0]);
        }
      });
    });

    effect(() => {
      const key = this.requestKey();
      if (!this.active() || !key) return;
      const request = untracked(this.request);
      if (request) untracked(() => this.transactionsStore.refreshTransactions(request));
    });
  }

  /** Chiamata dalla pagina Movimenti all'apertura; restituisce la funzione di disattivazione. */
  activate(): () => void {
    this.active.set(true);
    return () => this.active.set(false);
  }

  modifySelectedMonth(month: TransactionMonthOption) {
    this.selectedMonth.set(month);
    this.page.set(0);
  }

  // passando a "personalizzato" l'intervallo parte dal mese selezionato, se non ne è già impostato uno
  modifyPeriodMode(mode: PeriodMode) {
    const range = this.customRange();
    const month = this.selectedMonth();
    if (mode === "custom" && !range.from && !range.to && month) {
      this.customRange.set({ from: month.startDate.slice(0, 10), to: month.endDate.slice(0, 10) });
    }
    this.periodMode.set(mode);
    this.page.set(0);
  }

  modifyCustomRange(range: DateRange) {
    this.customRange.set(range);
    this.page.set(0);
  }

  modifyPage(page: number) {
    this.page.set(page);
  }

  modifyPageSize(pageSize: number) {
    this.page.set(0);
    this.pageSize.set(pageSize);
  }

  // stessa colonna: inverte la direzione; nuova colonna: parte dalla sua direzione naturale
  modifySort(field: TransactionSortField) {
    if (field === this.sortBy()) {
      this.sortDirection.update((current) => (current === "asc" ? "desc" : "asc"));
    } else {
      this.sortBy.set(field);
      this.sortDirection.set(defaultSortDirection(field));
    }
    this.page.set(0);
  }

  modifyMovementType(movementType: MovementType) {
    this.movementType.set(movementType);
    this.page.set(0);
  }

  modifySelectedAccountIds(accountIds: number[]) {
    this.selectedAccountIds.set(accountIds);
    this.page.set(0);
  }

  modifySelectedCategoryIds(categoryIds: number[]) {
    this.selectedCategoryIds.set(categoryIds);
    this.page.set(0);
  }
}
