import { describe, expect, it } from "vitest";
import { AnalyticsLayout } from "../models/AnalyticsLayout";
import {
  ALLOWED_WIDTHS,
  ANALYTICS_CARDS,
  DEFAULT_ANALYTICS_LAYOUT,
  normalizeLayout,
  sameLayout,
} from "./analytics-cards";

describe("analytics cards", () => {
  it("every default width is allowed for the card orientation", () => {
    for (const card of ANALYTICS_CARDS) {
      expect(ALLOWED_WIDTHS[card.orientation]).toContain(card.defaultWidth);
    }
  });

  it("null layout → default layout (a copy)", () => {
    const layout = normalizeLayout(null);
    expect(sameLayout(layout, DEFAULT_ANALYTICS_LAYOUT)).toBe(true);
    expect(layout[0]).not.toBe(DEFAULT_ANALYTICS_LAYOUT[0]);
  });

  it("keeps order, drops unknown and duplicate cards", () => {
    const raw: AnalyticsLayout = {
      cards: [
        { id: "category-rank", width: "half" },
        { id: "removed-card", width: "full" },
        { id: "cash-flow", width: "twoThirds" },
        { id: "category-rank", width: "third" },
      ],
    };
    expect(normalizeLayout(raw)).toEqual([
      { id: "category-rank", width: "half" },
      { id: "cash-flow", width: "twoThirds" },
    ]);
  });

  it("a width not allowed for the orientation falls back to the card default", () => {
    // le card verticali non possono occupare 2/3 o tutta la riga, quelle orizzontali non 1/3
    const raw: AnalyticsLayout = {
      cards: [
        { id: "period-rank", width: "full" },
        { id: "net-worth", width: "third" },
      ],
    };
    expect(normalizeLayout(raw)).toEqual([
      { id: "period-rank", width: "half" },
      { id: "net-worth", width: "half" },
    ]);
  });

  it("an empty saved layout stays empty (all cards hidden)", () => {
    expect(normalizeLayout({ cards: [] })).toEqual([]);
  });
});
