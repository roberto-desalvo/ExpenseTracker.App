import { TestBed } from "@angular/core/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { NotificationService } from "../api/notification.service";
import { OneDriveApi } from "../api/onedrive.api";
import { OneDriveConnection } from "../models/OneDriveConnection";
import { OneDriveStore } from "./onedrive.store";

const connection = (overrides: Partial<OneDriveConnection> = {}): OneDriveConnection => ({
  available: true,
  status: "NotConnected",
  bankFolders: ["BBVA", "Sella"],
  automaticSync: false,
  ...overrides,
});

const pending = (overrides: Partial<OneDriveConnection> = {}) =>
  connection({
    status: "Pending",
    userCode: "ABCD1234",
    verificationUri: "https://www.microsoft.com/link",
    codeExpiresOn: "2026-11-10T10:15:00Z",
    pollIntervalSeconds: 5,
    ...overrides,
  });

const connected = connection({ status: "Connected", driveOwner: "Mario Rossi", folderPath: "Apps/ExpenseTracker", automaticSync: true });

describe("OneDriveStore", () => {
  let store: OneDriveStore;
  const api = { get: vi.fn(), start: vi.fn(), complete: vi.fn(), disconnect: vi.fn(), sync: vi.fn() };
  const showSuccess = vi.fn();
  const showError = vi.fn();

  beforeEach(() => {
    vi.useFakeTimers();
    Object.values(api).forEach((mock) => mock.mockReset());
    showSuccess.mockReset();
    showError.mockReset();

    TestBed.configureTestingModule({
      providers: [
        { provide: OneDriveApi, useValue: api },
        { provide: NotificationService, useValue: { showSuccess, showError } },
      ],
    });
    store = TestBed.inject(OneDriveStore);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("checks at the interval given by the server until the user authorizes the app", async () => {
    api.start.mockResolvedValue(pending());
    api.complete.mockResolvedValueOnce(pending()).mockResolvedValueOnce(connected);

    await store.connect();
    expect(store.signingIn()).toBe(true);
    expect(store.connection()?.userCode).toBe("ABCD1234");

    await vi.advanceTimersByTimeAsync(4000);
    expect(api.complete).not.toHaveBeenCalled();

    await vi.advanceTimersByTimeAsync(1000);
    expect(api.complete).toHaveBeenCalledTimes(1);
    expect(store.signingIn()).toBe(true);

    await vi.advanceTimersByTimeAsync(5000);
    expect(store.signingIn()).toBe(false);
    expect(store.isConnected()).toBe(true);
    expect(showSuccess).toHaveBeenCalledWith("OneDrive collegato");

    await vi.advanceTimersByTimeAsync(20000);
    expect(api.complete).toHaveBeenCalledTimes(2);
  });

  it("goes back to the saved state when the sign-in is declined or the code expires", async () => {
    api.start.mockResolvedValue(pending());
    api.complete.mockRejectedValue(new Error("declined"));
    api.get.mockResolvedValue(connection());

    await store.connect();
    await vi.advanceTimersByTimeAsync(5000);

    expect(store.signingIn()).toBe(false);
    expect(store.status()).toBe("NotConnected");
    expect(api.complete).toHaveBeenCalledTimes(1);
  });

  it("resumes waiting when a sign-in started earlier is still valid", async () => {
    api.get.mockResolvedValue(pending());
    api.complete.mockResolvedValue(connected);

    const deactivate = store.activate();
    await vi.advanceTimersByTimeAsync(0);
    expect(store.signingIn()).toBe(true);

    deactivate();
    await vi.advanceTimersByTimeAsync(10000);
    expect(api.complete).not.toHaveBeenCalled();
  });

  it("cancel drops a connection that was never completed", async () => {
    api.start.mockResolvedValue(pending());
    api.disconnect.mockResolvedValue(connection());

    await store.connect();
    await store.cancel();

    expect(api.disconnect).toHaveBeenCalledTimes(1);
    expect(store.signingIn()).toBe(false);
    await vi.advanceTimersByTimeAsync(10000);
    expect(api.complete).not.toHaveBeenCalled();
  });

  it("cancel keeps an active connection that was being reconnected", async () => {
    api.start.mockResolvedValue(pending({ status: "Connected" }));

    await store.connect();
    await store.cancel();

    expect(api.disconnect).not.toHaveBeenCalled();
    expect(store.signingIn()).toBe(false);
    expect(store.isConnected()).toBe(true);
  });

  it("refreshes the state after a manual sync", async () => {
    api.get.mockResolvedValue(connected);
    api.sync.mockResolvedValue(undefined);

    await store.syncNow();
    expect(showSuccess).toHaveBeenCalledTimes(1);
    expect(api.get).not.toHaveBeenCalled();

    await vi.advanceTimersByTimeAsync(6000);
    expect(api.get).toHaveBeenCalledTimes(1);
  });
});
