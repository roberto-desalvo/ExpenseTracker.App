import { describe, expect, it } from "vitest";
import { amountText, budgetProgress, parseAmount, planningMonths } from "./budget-plan";

describe("planningMonths", () => {
  it("va dai mesi futuri ai passati, dal più recente", () => {
    const months = planningMonths(new Date(2026, 10, 8), 2, 1);
    expect(months.map((m) => m.startDate)).toEqual(["2026-12-01", "2026-11-01", "2026-10-01", "2026-09-01"]);
    expect(months[0].endDate).toBe("2026-12-31");
    expect(months[0].description).toBe("dicembre 2026");
  });
});

describe("parseAmount", () => {
  it("accetta virgola, punto e separatore delle migliaia", () => {
    expect(parseAmount("12,5")).toBe(12.5);
    expect(parseAmount("12.5")).toBe(12.5);
    expect(parseAmount("1.234,56 €")).toBe(1234.56);
    expect(parseAmount(" 300 ")).toBe(300);
  });

  it("vuoto = nessuna previsione, testo non valido = NaN", () => {
    expect(parseAmount("")).toBeNull();
    expect(parseAmount(null)).toBeNull();
    expect(parseAmount("abc")).toBeNaN();
    expect(parseAmount("-5")).toBeNaN();
  });

  it("amountText e parseAmount sono simmetrici", () => {
    expect(parseAmount(amountText(450.25))).toBe(450.25);
    expect(amountText(null)).toBe("");
  });
});

describe("budgetProgress", () => {
  it("sotto la previsione", () => {
    expect(budgetProgress(200, 150)).toEqual({ ratio: 0.75, fill: 75, diff: -50, over: false });
  });

  it("oltre la previsione la barra resta piena e segnala lo sforamento", () => {
    const progress = budgetProgress(450, 512)!;
    expect(progress.fill).toBe(100);
    expect(progress.over).toBe(true);
    expect(progress.diff).toBe(62);
  });

  it("senza previsione o senza spesa non c'è avanzamento", () => {
    expect(budgetProgress(null, 100)).toBeNull();
    expect(budgetProgress(0, 100)).toBeNull();
    expect(budgetProgress(100, null)).toBeNull();
  });
});
