import { describe, expect, it } from "vitest";
import { extractErrorMessage } from "./interceptors";

describe("extractErrorMessage", () => {
  it("preferisce detail, poi title, poi il primo errore di validazione", () => {
    expect(extractErrorMessage({ title: "T", detail: "D" })).toBe("D");
    expect(extractErrorMessage({ title: "T" })).toBe("T");
    expect(extractErrorMessage({ errors: { Name: ["Nome obbligatorio"] } })).toBe("Nome obbligatorio");
  });

  it("interpreta i problem details arrivati come testo JSON", () => {
    expect(extractErrorMessage('{"detail":"Conto non trovato"}')).toBe("Conto non trovato");
  });

  it("restituisce il testo grezzo se non è JSON e null se vuoto", () => {
    expect(extractErrorMessage("Errore interno")).toBe("Errore interno");
    expect(extractErrorMessage("")).toBeNull();
    expect(extractErrorMessage(null)).toBeNull();
  });
});
