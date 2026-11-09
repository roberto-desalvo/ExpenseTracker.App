import { describe, expect, it } from "vitest";
import { shouldSwap } from "./grid-sort";

const rect = (left: number, top: number, width: number, height: number) => ({
  left,
  top,
  right: left + width,
  bottom: top + height,
});

describe("shouldSwap", () => {
  describe("righe diverse (confronto verticale)", () => {
    const target = rect(0, 0, 400, 200); // card sopra il segnaposto
    const placeholder = rect(0, 212, 400, 120);

    it("verso l'alto scambia solo oltre la metà della card", () => {
      expect(shouldSwap({ x: 50, y: 150 }, target, placeholder, false)).toBe(false);
      expect(shouldSwap({ x: 50, y: 90 }, target, placeholder, false)).toBe(true);
    });

    it("dopo lo scambio il puntatore non riporta indietro la card più alta del segnaposto", () => {
      // la card ora è sotto il segnaposto, spostata solo dell'altezza del segnaposto: il puntatore (y 90) è ancora sopra di lei
      const moved = rect(0, 132, 400, 200);
      expect(shouldSwap({ x: 50, y: 140 }, moved, rect(0, 0, 400, 120), true)).toBe(false);
    });

    it("verso il basso scambia oltre la metà", () => {
      const below = rect(0, 300, 400, 200);
      const ph = rect(0, 0, 400, 288);
      expect(shouldSwap({ x: 50, y: 350 }, below, ph, true)).toBe(false);
      expect(shouldSwap({ x: 50, y: 420 }, below, ph, true)).toBe(true);
    });
  });

  describe("stessa riga (confronto orizzontale)", () => {
    const placeholder = rect(0, 0, 300, 200);
    const target = rect(312, 0, 300, 180);

    it("verso destra scambia oltre la metà", () => {
      expect(shouldSwap({ x: 400, y: 100 }, target, placeholder, true)).toBe(false);
      expect(shouldSwap({ x: 480, y: 100 }, target, placeholder, true)).toBe(true);
    });

    it("verso sinistra scambia prima della metà", () => {
      const left = rect(0, 0, 300, 180);
      const ph = rect(312, 0, 300, 200);
      expect(shouldSwap({ x: 200, y: 100 }, left, ph, false)).toBe(false);
      expect(shouldSwap({ x: 120, y: 100 }, left, ph, false)).toBe(true);
    });
  });
});
