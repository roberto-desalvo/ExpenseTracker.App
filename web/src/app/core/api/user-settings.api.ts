import { Injectable, inject } from "@angular/core";
import { apiConfig } from "../config/api";
import { AnalyticsLayout } from "../models/AnalyticsLayout";
import { CurrentUser } from "../models/CurrentUser";
import { AppearancePreferences } from "../theme/theme.service";
import { ApiClient } from "./api-client";

/** Profilo e preferenze dell'utente salvate a DB. */
@Injectable({ providedIn: "root" })
export class UserSettingsApi {
  private readonly api = inject(ApiClient);

  /** Profilo dell'utente autenticato (es. flag admin). */
  getMe(): Promise<CurrentUser> {
    return this.api.read("GET", apiConfig.userSettings.me, "Errore nel caricamento del profilo utente");
  }

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

  /** `null` se l'utente non ha mai salvato le preferenze di aspetto. */
  getAppearance(): Promise<Partial<AppearancePreferences> | null> {
    return this.api.read("GET", apiConfig.userSettings.appearance, "Errore nel caricamento delle preferenze di aspetto");
  }

  saveAppearance(preferences: AppearancePreferences): Promise<void> {
    return this.api.write(
      "PUT",
      apiConfig.userSettings.appearance,
      "Errore nel salvataggio delle preferenze di aspetto",
      preferences,
    );
  }
}
