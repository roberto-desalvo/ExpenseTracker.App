import { Injectable, inject } from "@angular/core";
import { apiConfig } from "../config/api";
import { BudgetMonth, BudgetMonthUpdate } from "../models/Budget";
import { ApiClient } from "./api-client";

@Injectable({ providedIn: "root" })
export class BudgetApi {
  private readonly api = inject(ApiClient);

  /** `month` in formato yyyy-MM-01. */
  getMonth(month: string): Promise<BudgetMonth> {
    return this.api.read(
      "GET",
      `${apiConfig.budget.base}?month=${encodeURIComponent(month)}`,
      "Errore nel caricamento della pianificazione",
    );
  }

  saveMonth(update: BudgetMonthUpdate): Promise<void> {
    return this.api.write("PUT", apiConfig.budget.base, "Errore nel salvataggio della pianificazione", update);
  }
}
