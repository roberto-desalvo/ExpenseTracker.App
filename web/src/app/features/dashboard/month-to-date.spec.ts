import { describe, expect, it } from "vitest";
import { DailyFlow, monthToDateAverages } from "./month-to-date";

const day = (period: string, spent: number, earned = 0, invested = 0): DailyFlow => ({ period, spent, earned, invested });

describe("monthToDateAverages", () => {
  it("considera ogni mese solo fino allo stesso giorno", () => {
    const today = new Date(2026, 10, 7); // 7 novembre
    const daily = [
      day("2026-10-01", 60, 600, 30),
      day("2026-10-07", 60), // incluso: stesso giorno
      day("2026-10-08", 1000), // escluso: dopo il 7
      day("2026-09-05", 120),
      day("2026-11-03", 999), // mese corrente: escluso
      day("2026-04-02", 999), // oltre i 6 mesi: escluso
    ];
    const result = monthToDateAverages(daily, today);
    expect(result.avgSpent).toBeCloseTo(240 / 6);
    expect(result.avgEarned).toBeCloseTo(600 / 6);
    expect(result.avgInvested).toBeCloseTo(30 / 6);
    expect(result.avgNet).toBeCloseTo((600 - 240) / 6);
  });

  it("nei mesi più corti si ferma all'ultimo giorno", () => {
    const today = new Date(2026, 2, 31); // 31 marzo
    const result = monthToDateAverages([day("2026-02-28", 60), day("2026-03-01", 999)], today);
    expect(result.avgSpent).toBeCloseTo(10);
  });

  it("senza dati le medie sono zero", () => {
    expect(monthToDateAverages([], new Date(2026, 0, 15))).toEqual({ avgSpent: 0, avgEarned: 0, avgInvested: 0, avgNet: 0 });
  });
});
