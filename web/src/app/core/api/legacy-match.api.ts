import { Injectable, inject } from "@angular/core";
import { apiConfig } from "../config/api";
import { LegacyCreateItem, LegacyMatchApplyItem, LegacyMonth, LegacySheet } from "../models/LegacyMatch";
import { ApiClient } from "./api-client";

/** Abbinamento storico Excel (solo admin): il file non viene salvato dal BE, va rimandato a ogni richiesta. */
@Injectable({ providedIn: "root" })
export class LegacyMatchApi {
  private readonly api = inject(ApiClient);

  getSheets(file: File): Promise<LegacySheet[]> {
    const form = new FormData();
    form.append("file", file, file.name);
    return this.api.upload(apiConfig.legacyMatch.sheets, "Errore nella lettura del file Excel", form);
  }

  getMonth(file: File, sheet: string): Promise<LegacyMonth> {
    const form = new FormData();
    form.append("file", file, file.name);
    form.append("sheet", sheet);
    return this.api.upload(apiConfig.legacyMatch.month, "Errore nel caricamento del mese", form);
  }

  apply(items: LegacyMatchApplyItem[]): Promise<number> {
    return this.api.read("POST", apiConfig.legacyMatch.apply, "Errore nel salvataggio degli abbinamenti", { items });
  }

  /** Crea i movimenti dei conti senza import automatico (Contanti, Hype). */
  create(items: LegacyCreateItem[]): Promise<number> {
    return this.api.read("POST", apiConfig.legacyMatch.create, "Errore nella creazione dei movimenti", { items });
  }
}
