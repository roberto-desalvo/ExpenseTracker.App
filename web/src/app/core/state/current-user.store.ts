import { Injectable, computed, inject, signal } from "@angular/core";
import { UserSettingsApi } from "../api/user-settings.api";
import { CurrentUser } from "../models/CurrentUser";

/** Profilo dell'utente autenticato letto dal BE (GET user/me), es. per mostrare le funzioni admin. */
@Injectable({ providedIn: "root" })
export class CurrentUserStore {
  private readonly api = inject(UserSettingsApi);
  private pending: Promise<void> | null = null;

  readonly user = signal<CurrentUser | null>(null);
  readonly isAdmin = computed(() => this.user()?.isAdmin === true);

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
