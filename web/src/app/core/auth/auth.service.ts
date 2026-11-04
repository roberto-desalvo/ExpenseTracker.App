import { Injectable, computed, signal } from "@angular/core";
import { EventMessage, EventType } from "@azure/msal-browser";
import { appEnv } from "../config/env";
import { loginRequest } from "./authConfig";
import { clearDemoSession, getDemoUserName, hasDemoSession, startDemoSession } from "./demoAuth";
import { clearDevUser, getDevUser, setDevUser } from "./devAuth";
import { msalInstance } from "./msalInstance";

export type AuthMode = "dev" | "demo" | "msal";

export interface AuthUser {
  mode: AuthMode;
  userName: string;
}

/**
 * Stato di autenticazione indipendente dal provider:
 * - sviluppo (bypass): solo nome utente, inviato come `DevUser`;
 * - produzione: sessione demo (token di breve durata emesso dal BE) oppure login Entra ID via MSAL.
 * `initialize()` va eseguito prima del render (gestisce anche il ritorno dal redirect di Entra ID).
 */
@Injectable({ providedIn: "root" })
export class AuthService {
  private readonly userState = signal<AuthUser | null>(null);
  private readonly loginErrorState = signal<string | null>(null);

  readonly user = this.userState.asReadonly();
  readonly loginError = this.loginErrorState.asReadonly();
  readonly isAuthenticated = computed(() => this.userState() !== null);
  readonly userName = computed(() => this.userState()?.userName ?? "");
  readonly devBypass = appEnv.devAuthBypass;
  readonly demoLoginEnabled = appEnv.demoLoginEnabled;

  async initialize(): Promise<void> {
    if (!msalInstance) {
      const devUser = getDevUser();
      this.userState.set(devUser ? { mode: "dev", userName: devUser } : null);
      return;
    }

    const instance = msalInstance;
    await instance.initialize();

    instance.addEventCallback((event: EventMessage) => {
      // In MSAL v5 anche il login via redirect fallito viene notificato come ACQUIRE_TOKEN_FAILURE.
      if (event.eventType === EventType.ACQUIRE_TOKEN_FAILURE && event.error) {
        this.loginErrorState.set(event.error.message);
      }
    });

    try {
      const result = await instance.handleRedirectPromise();
      if (result?.account) {
        instance.setActiveAccount(result.account);
      }
    } catch (error) {
      this.loginErrorState.set(error instanceof Error ? error.message : "Accesso non riuscito.");
    }

    if (hasDemoSession()) {
      this.userState.set({ mode: "demo", userName: getDemoUserName() });
      return;
    }

    const account = instance.getActiveAccount() ?? instance.getAllAccounts()[0];
    if (account) {
      instance.setActiveAccount(account);
      this.userState.set({ mode: "msal", userName: account.name ?? account.username ?? "" });
    }
  }

  /** Login di sviluppo con il solo nome utente. */
  loginDev(userName: string) {
    setDevUser(userName);
    this.userState.set({ mode: "dev", userName });
  }

  /** Avvia il login Entra ID (redirect). */
  loginEntra() {
    if (!msalInstance) return;
    this.loginErrorState.set(null);
    msalInstance.loginRedirect(loginRequest).catch((error: unknown) => {
      this.loginErrorState.set(error instanceof Error ? error.message : "Accesso non riuscito.");
    });
  }

  /** Avvia la demo con l'utente di test; rilancia l'errore per mostrarlo nella pagina di login. */
  async loginDemo(): Promise<void> {
    await startDemoSession();
    this.userState.set({ mode: "demo", userName: getDemoUserName() });
  }

  logout() {
    const user = this.userState();
    if (!user) return;

    switch (user.mode) {
      case "dev":
        clearDevUser();
        this.userState.set(null);
        break;
      case "demo":
        clearDemoSession();
        this.userState.set(null);
        break;
      case "msal":
        void msalInstance?.logoutRedirect();
        break;
    }
  }
}
