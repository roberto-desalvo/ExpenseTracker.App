import { Injectable, computed, inject, signal } from "@angular/core";
import { UserSettingsApi } from "../api/user-settings.api";
import {
  AnalyticsCardId,
  DEFAULT_ANALYTICS_LAYOUT,
  PlacedAnalyticsCard,
  normalizeLayout,
} from "../config/analytics-cards";

/** Card della pagina Analisi scelte dall'utente (ordine e larghezza), salvate a DB. */
@Injectable({ providedIn: "root" })
export class AnalyticsLayoutStore {
  private readonly api = inject(UserSettingsApi);
  private loading = false;

  readonly layout = signal<PlacedAnalyticsCard[]>(DEFAULT_ANALYTICS_LAYOUT.map((c) => ({ ...c })));
  readonly loaded = signal(false);
  readonly saving = signal(false);
  readonly visibleIds = computed(() => new Set<AnalyticsCardId>(this.layout().map((c) => c.id)));

  ensureLoaded() {
    if (this.loaded() || this.loading) return;
    this.loading = true;
    this.api
      .getAnalyticsLayout()
      .then((saved) => {
        this.layout.set(normalizeLayout(saved));
        this.loaded.set(true);
      })
      .catch(() => {
        // errore mostrato dal layer API: resta il layout predefinito, si riprova alla prossima richiesta
      })
      .finally(() => (this.loading = false));
  }

  isVisible(id: AnalyticsCardId) {
    return this.visibleIds().has(id);
  }

  /** Salva a DB; il layout in memoria cambia solo se il salvataggio riesce (l'errore è notificato dall'interceptor). */
  async save(cards: PlacedAnalyticsCard[]) {
    this.saving.set(true);
    try {
      const next = cards.map((c) => ({ id: c.id, width: c.width }));
      await this.api.saveAnalyticsLayout({ cards: next });
      this.layout.set(next);
      this.loaded.set(true);
    } finally {
      this.saving.set(false);
    }
  }
}
