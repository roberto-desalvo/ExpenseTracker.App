import { TestBed } from "@angular/core/testing";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LegacyMatchApi } from "../../core/api/legacy-match.api";
import { NotificationService } from "../../core/api/notification.service";
import { LegacyMonth } from "../../core/models/LegacyMatch";
import { TransactionsStore } from "../../core/state/transactions.store";
import { LegacyMatchStore, defaultPairDescription } from "./legacy-match.store";

const MONTH: LegacyMonth = {
  sheet: { name: "Ottobre2025", year: 2025, month: 10 },
  excelRows: [
    { rowNumber: 2, date: "2025-10-01", description: "spesa conad", amount: -18.6, accountName: "BBVA", accountId: 1, excelCategory: "Spesa", suggestedCategoryId: 7, toCreate: false },
    { rowNumber: 3, date: "2025-10-02", description: "gtt", amount: -1.9, accountName: "BBVA", accountId: 1, excelCategory: "Spostamenti", suggestedCategoryId: null, toCreate: false },
    { rowNumber: 4, date: "2025-10-04", description: "caffè", amount: -1.2, accountName: "Contanti", accountId: 2, excelCategory: "Mangiare/Bere fuori", suggestedCategoryId: 9, toCreate: true },
    { rowNumber: 5, date: "2025-10-05", description: "pizza", amount: -8, accountName: "Hype", accountId: 3, excelCategory: null, suggestedCategoryId: null, toCreate: true },
    { rowNumber: 6, date: "2025-10-06", description: "regalo", amount: -5, accountName: "Contanti", accountId: null, excelCategory: null, suggestedCategoryId: null, toCreate: true },
  ],
  dbTransactions: [
    { id: 10, date: "2025-10-01", description: "POS CONAD", amount: -18.6, accountId: 1, account: "BBVA", categoryId: null, category: null, isTransfer: false, legacyMatchedAt: null },
    { id: 11, date: "2025-10-03", description: "GTT TORINO", amount: -1.9, accountId: 1, account: "BBVA", categoryId: null, category: null, isTransfer: false, legacyMatchedAt: null },
    { id: 12, date: "2025-10-20", description: "Pizza ", amount: -8, accountId: 3, account: "Hype", categoryId: null, category: null, isTransfer: false, legacyMatchedAt: null },
  ],
  suggestions: [{ rowNumber: 2, transactionId: 10 }],
};

describe("LegacyMatchStore", () => {
  let store: LegacyMatchStore;
  const getSheets = vi.fn();
  const getMonth = vi.fn();
  const apply = vi.fn();
  const create = vi.fn();
  const showSuccess = vi.fn();
  const showError = vi.fn();
  const notifyExternalChange = vi.fn();
  const file = new File(["x"], "Spese.xlsx");

  beforeEach(async () => {
    vi.clearAllMocks();
    sessionStorage.clear();
    getSheets.mockResolvedValue([{ name: "Ottobre2025", year: 2025, month: 10 }]);
    getMonth.mockResolvedValue(structuredClone(MONTH));
    apply.mockResolvedValue(1);
    create.mockResolvedValue(1);

    TestBed.configureTestingModule({
      providers: [
        LegacyMatchStore,
        { provide: LegacyMatchApi, useValue: { getSheets, getMonth, apply, create } },
        { provide: NotificationService, useValue: { showSuccess, showError } },
        { provide: TransactionsStore, useValue: { notifyExternalChange } },
      ],
    });
    store = TestBed.inject(LegacyMatchStore);
    await store.openFile(file);
  });

  it("opens the first sheet and builds pairs from the suggestions", () => {
    expect(getMonth).toHaveBeenCalledWith(file, "Ottobre2025");
    expect(store.pairList()).toEqual([
      { rowNumber: 2, transactionId: 10, description: "POS CONAD - spesa conad", categoryId: 7, included: true, suggested: true },
    ]);
    expect(store.pairIndexByRow().get(2)).toBe(1);
  });

  it("pairs a selected row with the clicked transaction, replacing the transaction's previous pair", () => {
    store.selectRow(3);
    store.selectTransaction(10);

    expect(store.pairList().map((p) => [p.rowNumber, p.transactionId])).toEqual([[3, 10]]);
    expect(store.pairs().get(3)).toMatchObject({ description: "POS CONAD - gtt", categoryId: null, suggested: false });
    expect(store.selectedRow()).toBeNull();
  });

  it("selects the paired row when a paired transaction is clicked without a selection", () => {
    store.selectTransaction(10);

    expect(store.selectedRow()).toBe(2);
  });

  it("saves only included pairs with the edited values and reloads the month", async () => {
    store.selectRow(3);
    store.selectTransaction(11);
    store.updatePair(2, { included: false });
    store.updatePair(3, { description: "  GTT - biglietto ", categoryId: 4 });

    await store.save();

    expect(apply).toHaveBeenCalledWith([{ transactionId: 11, description: "GTT - biglietto", categoryId: 4 }]);
    expect(showSuccess).toHaveBeenCalled();
    expect(notifyExternalChange).toHaveBeenCalled();
    expect(getMonth).toHaveBeenCalledTimes(2);
  });

  it("does not save when a description is empty", async () => {
    store.updatePair(2, { description: "   " });

    await store.save();

    expect(apply).not.toHaveBeenCalled();
    expect(showError).toHaveBeenCalled();
  });

  it("leaves only unhandled rows among the pending ones", () => {
    expect(store.pendingRows().map((r) => r.rowNumber)).toEqual([3]);

    store.unpair(2);

    expect(store.pendingRows().map((r) => r.rowNumber)).toEqual([2, 3]);
  });

  it("marks a row as matched without a transaction, removing its pair", () => {
    store.markAsMatched(2);
    store.markAsMatched(3);

    expect(store.pairs().size).toBe(0);
    expect(store.pendingRows()).toEqual([]);

    store.restoreRow(3);

    expect(store.pendingRows().map((r) => r.rowNumber)).toEqual([3]);
  });

  it("keeps saved and dismissed rows handled when the sheet is reloaded in the same session", async () => {
    store.markAsMatched(3);
    await store.save();

    expect(store.savedRows().has(2)).toBe(true);
    expect(store.dismissedRows().has(3)).toBe(true);
    // il mese ricaricato ripropone lo stesso suggerimento, ma la riga salvata non torna in abbinamento
    expect(store.pairs().size).toBe(0);
    expect(store.pendingRows()).toEqual([]);

    // pagina riaperta: nuovo store, stato letto da sessionStorage
    const reopened = TestBed.runInInjectionContext(() => new LegacyMatchStore());
    await reopened.openFile(file);
    expect(reopened.pairs().size).toBe(0);
    expect(reopened.pendingRows()).toEqual([]);
  });

  it("keeps cash and Hype rows out of the matching list and opens the create dialog", () => {
    expect(store.matchRows().map((r) => r.rowNumber)).toEqual([2, 3]);
    expect(store.createRows().map((r) => r.rowNumber)).toEqual([4, 5, 6]);
    expect(store.createDialogOpen()).toBe(true);
  });

  it("flags a duplicate with same account, amount and name, and leaves it excluded by default", () => {
    expect([...store.duplicates().keys()]).toEqual([5]);
    expect(store.duplicates().get(5)?.id).toBe(12);
    expect(store.drafts().get(5)?.included).toBe(false);
    expect(store.drafts().get(6)?.included).toBe(false); // conto non trovato

    store.updateDraft(5, { description: "pizza da Gino" });
    // il nome Excel originale resta uguale a quello a DB: il warning rimane
    expect(store.duplicates().has(5)).toBe(true);

    store.updateDraft(4, { description: "Pizza" });
    expect(store.duplicates().has(4)).toBe(false); // conto diverso
  });

  it("creates the included rows with the edited values and hides them", async () => {
    store.updateDraft(4, { description: " caffè bar ", categoryId: 3 });

    const message = await store.createDrafts();

    expect(create).toHaveBeenCalledWith([
      { accountId: 2, date: "2025-10-04", amount: -1.2, description: "caffè bar", categoryId: 3 },
    ]);
    expect(message).toBe("1 movimento creato");
    expect(store.savedRows().has(4)).toBe(true);
    await vi.waitFor(() => expect(getMonth).toHaveBeenCalledTimes(2));
    expect(store.createRows().map((r) => r.rowNumber)).toEqual([5, 6]);
    expect(store.createDialogOpen()).toBe(true); // resta com'era: la chiude la modale
  });

  it("warns when a transaction of the same month, account and amount already contains the Excel description", () => {
    expect(store.existingMatches().size).toBe(0);

    // movimento salvato in una sessione precedente come "DB - Excel"
    const previous = { ...MONTH.dbTransactions[1], id: 20, description: "GTT TORINO - gtt", legacyMatchedAt: "2026-11-01T10:00:00Z" };
    store.month.update((m) => m && { ...m, dbTransactions: [...m.dbTransactions, previous] });

    expect(store.existingMatches().get(3)?.id).toBe(20);
  });

  it("ignores transactions never matched, even if their description contains the Excel text", () => {
    store.month.update(
      (m) => m && { ...m, dbTransactions: m.dbTransactions.map((t) => (t.id === 10 ? { ...t, description: "SPESA CONAD CORSO" } : t)) },
    );
    expect(store.existingMatches().has(2)).toBe(false);
    expect(store.existingMatches().has(3)).toBe(false); // "GTT TORINO" contiene "gtt" ma non è mai stato abbinato

    store.month.update(
      (m) => m && { ...m, dbTransactions: m.dbTransactions.map((t) => (t.id === 10 ? { ...t, legacyMatchedAt: "2026-11-01T10:00:00Z" } : t)) },
    );
    expect(store.existingMatches().get(2)?.id).toBe(10);
  });

  it("builds the default description from the non-empty parts", () => {
    expect(defaultPairDescription(MONTH.excelRows[0], { ...MONTH.dbTransactions[0], description: " " })).toBe("spesa conad");
  });
});
