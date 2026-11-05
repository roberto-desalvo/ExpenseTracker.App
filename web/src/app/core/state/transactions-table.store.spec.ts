import { TestBed } from "@angular/core/testing";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AccountsStore } from "./accounts.store";
import { CategoriesStore } from "./categories.store";
import { TransactionsTableStore } from "./transactions-table.store";
import { TransactionsStore } from "./transactions.store";
import { signal } from "@angular/core";

const MONTHS = [
  { startDate: "2026-11-01T00:00:00", endDate: "2026-11-30T23:59:59", description: "novembre 2026" },
  { startDate: "2026-10-01T00:00:00", endDate: "2026-10-31T23:59:59", description: "ottobre 2026" },
];

describe("TransactionsTableStore", () => {
  let store: TransactionsTableStore;
  const refreshTransactions = vi.fn();

  beforeEach(() => {
    refreshTransactions.mockReset();
    TestBed.configureTestingModule({
      providers: [
        { provide: AccountsStore, useValue: { all: signal([{ id: 1, name: "A" }, { id: 2, name: "B" }]) } },
        { provide: CategoriesStore, useValue: { all: signal([{ id: 10, name: "C" }]) } },
        {
          provide: TransactionsStore,
          useValue: { availableMonths: signal(MONTHS), availableMonthsLoading: signal(false), refreshTransactions },
        },
      ],
    });
    store = TestBed.inject(TransactionsTableStore);
    TestBed.tick();
  });

  it("seleziona il mese più recente e costruisce la richiesta senza filtri", () => {
    expect(store.selectedMonth()?.startDate).toBe(MONTHS[0].startDate);
    expect(store.request()).toMatchObject({
      fromDate: MONTHS[0].startDate,
      toDate: MONTHS[0].endDate,
      idAccounts: null,
      idCategories: null,
      isIncome: null,
      sortBy: "date",
      sortDirection: "desc",
      page: 0,
    });
  });

  it("tutti i conti selezionati equivalgono a nessun filtro", () => {
    store.modifySelectedAccountIds([1, 2]);
    expect(store.request()?.idAccounts).toBeNull();
    store.modifySelectedAccountIds([2]);
    expect(store.request()?.idAccounts).toEqual([2]);
  });

  it("l'ordinamento sulla stessa colonna inverte la direzione e riparte dalla prima pagina", () => {
    store.modifyPage(3);
    store.modifySort("date");
    expect(store.sortDirection()).toBe("asc");
    expect(store.page()).toBe(0);
    store.modifySort("description");
    expect(store.sortBy()).toBe("description");
    expect(store.sortDirection()).toBe("asc");
  });

  it("passando a personalizzato l'intervallo parte dal mese selezionato", () => {
    store.modifyPeriodMode("custom");
    expect(store.customRange()).toEqual({ from: "2026-11-01", to: "2026-11-30" });
    expect(store.request()?.fromDate).toBe("2026-11-01T00:00:00");
    expect(store.request()?.toDate).toBe("2026-11-30T23:59:59.999");
  });

  it("drill-down su un mese disponibile: vista per mese con categoria e tipo, conti azzerati", () => {
    store.modifySelectedAccountIds([2]);
    store.modifyPage(2);
    store.applyDrillDown({ period: "2026-10", categoryIds: [10], movementType: "outcomes" });
    TestBed.tick();
    expect(store.periodMode()).toBe("month");
    expect(store.selectedMonth()?.startDate).toBe(MONTHS[1].startDate);
    expect(store.request()).toMatchObject({
      fromDate: MONTHS[1].startDate,
      idAccounts: null,
      isIncome: false,
      page: 0,
    });
    expect(store.selectedCategoryIds()).toEqual([10]);
  });

  it("drill-down su un mese non disponibile: intervallo personalizzato", () => {
    store.applyDrillDown({ period: "2025-03", categoryIds: [], movementType: "incomes" });
    TestBed.tick();
    expect(store.periodMode()).toBe("custom");
    expect(store.customRange()).toEqual({ from: "2025-03-01", to: "2025-03-31" });
    expect(store.request()).toMatchObject({ fromDate: "2025-03-01T00:00:00", isIncome: true });
  });

  it("drill-down su una settimana: intervallo personalizzato", () => {
    store.applyDrillDown({ period: "2026-W45", categoryIds: [], movementType: "all" });
    expect(store.periodMode()).toBe("custom");
    expect(store.customRange()).toEqual({ from: "2026-11-02", to: "2026-11-08" });
  });

  it("carica la lista solo mentre la pagina è attiva", () => {
    TestBed.tick();
    expect(refreshTransactions).not.toHaveBeenCalled();
    const deactivate = store.activate();
    TestBed.tick();
    expect(refreshTransactions).toHaveBeenCalledTimes(1);
    store.modifyMovementType("incomes");
    TestBed.tick();
    expect(refreshTransactions).toHaveBeenCalledTimes(2);
    expect(refreshTransactions.mock.lastCall?.[0]).toMatchObject({ isIncome: true });
    deactivate();
    store.modifyMovementType("outcomes");
    TestBed.tick();
    expect(refreshTransactions).toHaveBeenCalledTimes(2);
  });
});
