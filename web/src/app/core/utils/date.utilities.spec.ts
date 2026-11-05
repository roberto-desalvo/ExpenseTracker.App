import { describe, expect, it } from "vitest";
import { periodRange } from "./date.utilities";

describe("periodRange", () => {
  it("mese: dal primo all'ultimo giorno", () => {
    expect(periodRange("2024-02")).toEqual({ from: "2024-02-01", to: "2024-02-29" });
    expect(periodRange("2026-12")).toEqual({ from: "2026-12-01", to: "2026-12-31" });
  });

  it("settimana ISO, anche a cavallo d'anno", () => {
    expect(periodRange("2026-W01")).toEqual({ from: "2025-12-29", to: "2026-01-04" });
    expect(periodRange("2026-W45")).toEqual({ from: "2026-11-02", to: "2026-11-08" });
    expect(periodRange("2020-W53")).toEqual({ from: "2020-12-28", to: "2021-01-03" });
  });

  it("giorno e anno", () => {
    expect(periodRange("2026-05-17")).toEqual({ from: "2026-05-17", to: "2026-05-17" });
    expect(periodRange("2025")).toEqual({ from: "2025-01-01", to: "2025-12-31" });
  });

  it("chiave non riconosciuta", () => {
    expect(periodRange("maggio")).toBeNull();
  });
});
