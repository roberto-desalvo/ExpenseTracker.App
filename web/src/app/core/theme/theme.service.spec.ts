import { TestBed } from "@angular/core/testing";
import { afterEach, describe, expect, it } from "vitest";
import { ThemeService, buildAppTokens } from "./theme.service";

describe("ThemeService", () => {
  afterEach(() => localStorage.clear());

  it("applica i token dello stile come variabili CSS su :root", () => {
    const service = TestBed.inject(ThemeService);
    service.setPreset("ledger");
    service.setModePreference("dark");
    TestBed.tick();

    const expected = buildAppTokens("ledger", "dark");
    const root = document.documentElement;
    expect(root.style.getPropertyValue("--app-primary")).toBe(expected.primary);
    expect(root.style.getPropertyValue("--mat-sys-primary")).toBe(expected.primary);
    expect(root.style.getPropertyValue("--app-radius-card")).toBe(`${expected.radiusCard}px`);
    expect(root.dataset["mode"]).toBe("dark");
  });

  it("salva le preferenze e scarta valori non validi in lettura", () => {
    localStorage.setItem("expense-tracker:appearance", JSON.stringify({ preset: "boh", mode: "dark", nav: "topbar" }));
    const service = TestBed.inject(ThemeService);
    expect(service.preferences()).toEqual({ preset: "aurora", mode: "dark", nav: "topbar" });

    service.toggleTheme();
    TestBed.tick();
    expect(JSON.parse(localStorage.getItem("expense-tracker:appearance") ?? "{}").mode).toBe("light");
  });
});
