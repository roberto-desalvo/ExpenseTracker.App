import { Injectable, effect, inject, untracked } from "@angular/core";
import { UserSettingsApi } from "../api/user-settings.api";
import { AppearancePreferences, ThemeService } from "../theme/theme.service";

const same = (a: AppearancePreferences, b: AppearancePreferences) =>
  a.preset === b.preset && a.mode === b.mode && a.nav === b.nav;

/**
 * Sincronizza le preferenze di aspetto di `ThemeService` con il DB: dopo l'accesso carica quelle salvate
 * (assenti = predefinite) e da lì in poi salva ogni modifica. Prima del caricamento vale la cache in localStorage.
 */
@Injectable({ providedIn: "root" })
export class AppearanceStore {
  private readonly api = inject(UserSettingsApi);
  private readonly theme = inject(ThemeService);

  private pending: Promise<void> | null = null;
  /** ultimo valore noto a DB; null finché non è stato caricato */
  private synced: AppearancePreferences | null = null;
  private saving = false;

  constructor() {
    effect(() => {
      this.theme.preferences();
      untracked(() => void this.flush());
    });
  }

  /** Carica le preferenze una volta; in caso di errore (già notificato) si riprova alla chiamata successiva. */
  ensureLoaded(): Promise<void> {
    if (this.synced) return Promise.resolve();
    this.pending ??= this.api
      .getAppearance()
      .then((saved) => {
        this.theme.replacePreferences(saved);
        this.synced = this.theme.preferences();
      })
      .catch(() => undefined)
      .finally(() => (this.pending = null));
    return this.pending;
  }

  /** Salva lo stato corrente se diverso da quello a DB; un salvataggio alla volta, l'ultimo valore vince. */
  private async flush() {
    if (!this.synced || this.saving) return;
    const current = this.theme.preferences();
    if (same(current, this.synced)) return;

    this.saving = true;
    try {
      await this.api.saveAppearance(current);
      this.synced = current;
    } catch {
      // errore notificato dall'interceptor: le preferenze restano applicate localmente
      return;
    } finally {
      this.saving = false;
    }
    await this.flush();
  }
}
