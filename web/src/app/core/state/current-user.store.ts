import { Injectable, computed, inject, signal } from "@angular/core";
import { UserSettingsApi } from "../api/user-settings.api";
import { AuthService } from "../auth/auth.service";
import { CurrentUser } from "../models/CurrentUser";

/** Profilo dell'utente autenticato letto dal BE (GET user/me), es. per mostrare le funzioni admin. */
@Injectable({ providedIn: "root" })
export class CurrentUserStore {
  private readonly api = inject(UserSettingsApi);
  private readonly auth = inject(AuthService);
  private pending: Promise<void> | null = null;

  readonly user = signal<CurrentUser | null>(null);
  readonly isAdmin = computed(() => this.user()?.isAdmin === true);
  /** Sessione demo, o utente demo del BE (es. login di sviluppo): niente import di file né collegamento OneDrive. */
  readonly isDemo = computed(() => this.auth.user()?.mode === "demo" || this.user()?.isDemo === true);

  /** Carica il profilo una volta; in caso di errore (già notificato) si riprova alla chiamata successiva. */
  ensureLoaded(): Promise<void> {
    if (this.user()) return Promise.resolve();
    this.pending ??= this.api
      .getMe()
      .then((user) => this.user.set(user))
      .catch(() => undefined)
      .finally(() => (this.pending = null));
    return this.pending;
  }
}
