import { Injectable, inject } from "@angular/core";
import { apiConfig } from "../config/api";
import { AnalyticsLayout } from "../models/AnalyticsLayout";
import { ApiClient } from "./api-client";

/** Preferenze dell'utente salvate a DB. */
@Injectable({ providedIn: "root" })
export class UserSettingsApi {
  private readonly api = inject(ApiClient);

  /** `null` se l'utente non ha mai personalizzato il layout. */
  getAnalyticsLayout(): Promise<AnalyticsLayout | null> {
    return this.api.read("GET", apiConfig.userSettings.analyticsLayout, "Errore nel caricamento del layout di Analisi");
  }

  saveAnalyticsLayout(layout: AnalyticsLayout): Promise<void> {
    return this.api.write(
      "PUT",
      apiConfig.userSettings.analyticsLayout,
      "Errore nel salvataggio del layout di Analisi",
      layout,
    );
  }
}
