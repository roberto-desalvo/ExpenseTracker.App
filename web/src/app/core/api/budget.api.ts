import { Injectable, inject } from "@angular/core";
import { apiConfig } from "../config/api";
import { BudgetMonth, BudgetMonthUpdate, BudgetTotal } from "../models/Budget";
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

  /** Totali previsti dei mesi toccati dall'intervallo (date yyyy-MM-dd). */
  getTotals(startDate: string, endDate: string): Promise<BudgetTotal[]> {
    const query = `startDate=${encodeURIComponent(startDate)}&endDate=${encodeURIComponent(endDate)}`;
    return this.api.read("GET", `${apiConfig.budget.totals}?${query}`, "Errore nel caricamento delle pianificazioni");
  }

  saveMonth(update: BudgetMonthUpdate): Promise<void> {
    return this.api.write("PUT", apiConfig.budget.base, "Errore nel salvataggio della pianificazione", update);
  }
}
