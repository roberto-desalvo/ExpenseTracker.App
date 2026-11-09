import { describe, expect, it } from "vitest";
import { periodKeyOf, planVsActual } from "./plan-vs-actual";

describe("periodKeyOf", () => {
  it("usa le settimane ISO", () => {
    expect(periodKeyOf(new Date(2026, 0, 1), 2)).toBe("2026-W01");
    expect(periodKeyOf(new Date(2027, 0, 1), 2)).toBe("2026-W53");
    expect(periodKeyOf(new Date(2026, 10, 9), 2)).toBe("2026-W46");
    expect(periodKeyOf(new Date(2026, 10, 15), 2)).toBe("2026-W46");
  });

  it("giorno, mese e anno", () => {
    const date = new Date(2026, 1, 3);
    expect(periodKeyOf(date, 1)).toBe("2026-02-03");
    expect(periodKeyOf(date, 3)).toBe("2026-02");
    expect(periodKeyOf(date, 4)).toBe("2026");
  });
});

describe("planVsActual", () => {
  const planned = new Map([
    ["2026-09", 300],
    ["2026-10", 310],
  ]);
  const spent = new Map([
    ["2026-08-20", 999],
    ["2026-09-10", 100],
    ["2026-10-01", 50],
    ["2026-10-31", 20],
  ]);

  it("per mese: previsione intera dei mesi completi, mesi senza previsione omessi", () => {
    expect(planVsActual("2026-08-01", "2026-10-31", 3, planned, spent)).toEqual([
      { period: "2026-09", planned: 300, spent: 100 },
      { period: "2026-10", planned: 310, spent: 70 },
    ]);
  });

  it("per anno: somma solo i mesi pianificati", () => {
    const [year] = planVsActual("2026-01-01", "2026-12-31", 4, planned, spent);
    expect(year.period).toBe("2026");
    expect(year.planned).toBeCloseTo(610);
    expect(year.spent).toBe(170);
  });

  it("per giorno: la previsione del mese è divisa in parti uguali", () => {
    const days = planVsActual("2026-10-01", "2026-10-02", 1, planned, spent);
    expect(days).toEqual([
      { period: "2026-10-01", planned: 10, spent: 50 },
      { period: "2026-10-02", planned: 10, spent: 0 },
    ]);
  });

  it("un mese tagliato dall'intervallo riceve la quota dei suoi giorni", () => {
    const [october] = planVsActual("2026-10-22", "2026-10-31", 3, planned, spent);
    expect(october.planned).toBeCloseTo(100);
    expect(october.spent).toBe(20);
  });

  it("per settimana: le settimane a cavallo di due mesi sommano le quote di entrambi", () => {
    // 2026-W40: lun 28/9 – dom 4/10 → 3 giorni di settembre (10/giorno) + 4 di ottobre (10/giorno)
    const week = planVsActual("2026-09-28", "2026-10-04", 2, planned, spent);
    expect(week).toHaveLength(1);
    expect(week[0].period).toBe("2026-W40");
    expect(week[0].planned).toBeCloseTo(70);
    expect(week[0].spent).toBe(50);
  });
});
