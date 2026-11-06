import { TestBed } from "@angular/core/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { UserSettingsApi } from "../api/user-settings.api";
import { DEFAULT_APPEARANCE, ThemeService } from "../theme/theme.service";
import { AppearanceStore } from "./appearance.store";

describe("AppearanceStore", () => {
  const getAppearance = vi.fn();
  const saveAppearance = vi.fn();

  beforeEach(() => {
    getAppearance.mockReset();
    saveAppearance.mockReset().mockResolvedValue(undefined);
    TestBed.configureTestingModule({
      providers: [{ provide: UserSettingsApi, useValue: { getAppearance, saveAppearance } }],
    });
  });

  afterEach(() => localStorage.clear());

  it("applica le preferenze salvate a DB senza risalvarle", async () => {
    getAppearance.mockResolvedValue({ preset: "ledger", mode: "dark", nav: "topbar" });
    const store = TestBed.inject(AppearanceStore);
    const theme = TestBed.inject(ThemeService);

    await store.ensureLoaded();
    TestBed.tick();

    expect(theme.preferences()).toEqual({ preset: "ledger", mode: "dark", nav: "topbar" });
    expect(saveAppearance).not.toHaveBeenCalled();
  });

  it("senza preferenze a DB torna a quelle predefinite, anche se la cache locale è diversa", async () => {
    localStorage.setItem("expense-tracker:appearance", JSON.stringify({ preset: "graphite", mode: "dark", nav: "topbar" }));
    getAppearance.mockResolvedValue(null);
    const store = TestBed.inject(AppearanceStore);
    const theme = TestBed.inject(ThemeService);

    await store.ensureLoaded();
    TestBed.tick();

    expect(theme.preferences()).toEqual(DEFAULT_APPEARANCE);
    expect(saveAppearance).not.toHaveBeenCalled();
  });

  it("salva le modifiche solo dopo il caricamento", async () => {
    getAppearance.mockResolvedValue(null);
    const store = TestBed.inject(AppearanceStore);
    const theme = TestBed.inject(ThemeService);

    theme.setPreset("ledger");
    TestBed.tick();
    expect(saveAppearance).not.toHaveBeenCalled();

    await store.ensureLoaded();
    theme.setModePreference("dark");
    TestBed.tick();
    await Promise.resolve();

    expect(saveAppearance).toHaveBeenCalledOnce();
    expect(saveAppearance).toHaveBeenCalledWith({ ...DEFAULT_APPEARANCE, mode: "dark" });
  });
});
