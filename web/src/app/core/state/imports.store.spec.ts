import { computed, signal } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ImportApi } from "../api/import.api";
import { NotificationService } from "../api/notification.service";
import { AuthService } from "../auth/auth.service";
import Account from "../models/Account";
import { AccountType } from "../models/AccountType";
import { ImportLog } from "../models/ImportLog";
import { AccountsStore } from "./accounts.store";
import { AccountTypesStore } from "./account-types.store";
import { IMPORT_POLL_INTERVAL_MS, ImportsStore } from "./imports.store";
import { TransactionsStore } from "./transactions.store";

const TYPES: AccountType[] = [
  { id: 1, name: "BBVA", supportsImport: true },
  { id: 5, name: "Revolut", supportsImport: false },
];

const ACCOUNTS: Account[] = [
  { id: 10, name: "Conto BBVA", accountTypeId: 1 },
  { id: 11, name: "Revolut", accountTypeId: 5 },
  { id: 12, name: "Contanti" },
];

const log = (id: number, status: ImportLog["status"], newTransactionsCount = 0): ImportLog => ({
  id,
  importedOn: "2026-11-05T10:00:00Z",
  status,
  source: "BBVA CSV",
  fileName: `file-${id}.csv`,
  accountId: 10,
  accountName: "Conto BBVA",
  newTransactionsCount,
  newTransfersCount: 0,
  skippedDuplicatesCount: 0,
});

describe("ImportsStore", () => {
  let store: ImportsStore;
  const query = vi.fn();
  const upload = vi.fn();
  const notifyExternalChange = vi.fn();
  const showSuccess = vi.fn();
  const showError = vi.fn();
  const user = signal<{ mode: string; userName: string } | null>({ mode: "msal", userName: "Roberto" });

  beforeEach(() => {
    vi.useFakeTimers();
    query.mockReset();
    upload.mockReset();
    notifyExternalChange.mockReset();
    showSuccess.mockReset();
    showError.mockReset();
    user.set({ mode: "msal", userName: "Roberto" });

    const typesById = computed(() => new Map(TYPES.map((t) => [t.id, t])));
    TestBed.configureTestingModule({
      providers: [
        { provide: ImportApi, useValue: { query, upload } },
        { provide: AccountsStore, useValue: { all: signal(ACCOUNTS) } },
        { provide: AccountTypesStore, useValue: { all: signal(TYPES), byId: typesById } },
        { provide: AuthService, useValue: { user } },
        { provide: TransactionsStore, useValue: { notifyExternalChange } },
        { provide: NotificationService, useValue: { showSuccess, showError } },
      ],
    });
    store = TestBed.inject(ImportsStore);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("considera importabili solo i conti di un tipo che prevede l'import", () => {
    expect(store.importableAccounts().map((a) => a.id)).toEqual([10]);
    expect(store.canImport()).toBe(true);
  });

  it("non permette l'import all'utente demo", () => {
    user.set({ mode: "demo", userName: "Demo" });
    expect(store.canImport()).toBe(false);
  });

  it("ricontrolla gli import in corso e si ferma quando sono conclusi", async () => {
    query.mockResolvedValueOnce({ items: [log(1, "Processing")], totalCount: 1, page: 0, pageSize: 20 });
    query.mockResolvedValueOnce({ items: [log(1, "Completed", 3)], totalCount: 1, page: 0, pageSize: 20 });

    store.activate();
    await vi.waitFor(() => expect(store.activeCount()).toBe(1));

    await vi.advanceTimersByTimeAsync(IMPORT_POLL_INTERVAL_MS);
    await vi.waitFor(() => expect(store.activeCount()).toBe(0));

    expect(query).toHaveBeenCalledTimes(2);
    expect(notifyExternalChange).toHaveBeenCalledTimes(1);
    expect(showSuccess).toHaveBeenCalledWith(expect.stringContaining("3 nuovi movimenti"));

    await vi.advanceTimersByTimeAsync(IMPORT_POLL_INTERVAL_MS * 3);
    expect(query).toHaveBeenCalledTimes(2);
  });

  it("non fa polling se non ci sono import in corso", async () => {
    query.mockResolvedValue({ items: [log(1, "Completed")], totalCount: 1, page: 0, pageSize: 20 });

    store.activate();
    await vi.waitFor(() => expect(store.logs()).toHaveLength(1));
    await vi.advanceTimersByTimeAsync(IMPORT_POLL_INTERVAL_MS * 3);

    expect(query).toHaveBeenCalledTimes(1);
  });

  it("segue un file caricato anche fuori dalla pagina e segnala un import fallito", async () => {
    upload.mockResolvedValue(log(7, "Queued"));
    query.mockResolvedValueOnce({ items: [{ ...log(7, "Failed"), errorMessage: "Invalid header" }], totalCount: 1, page: 0, pageSize: 20 });

    await store.upload(10, new File(["a"], "file-7.csv"));
    expect(query).not.toHaveBeenCalled();

    await vi.advanceTimersByTimeAsync(IMPORT_POLL_INTERVAL_MS);
    await vi.waitFor(() => expect(showError).toHaveBeenCalledWith(expect.stringContaining("Invalid header")));

    expect(notifyExternalChange).not.toHaveBeenCalled();
    // fuori dalla pagina lo storico non viene popolato
    expect(store.logs()).toHaveLength(0);
  });
});
