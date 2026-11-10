import { Injectable, computed, inject, signal } from "@angular/core";
import { NotificationService } from "../api/notification.service";
import { OneDriveApi } from "../api/onedrive.api";
import { OneDriveConnection } from "../models/OneDriveConnection";

/** Attesa prima di rileggere lo stato dopo una sincronizzazione manuale (elaborata in coda). */
export const ONEDRIVE_SYNC_REFRESH_MS = 6000;

/**
 * Collegamento del OneDrive dell'utente. L'accesso usa il device code flow: il BE emette un codice che l'utente
 * inserisce sulla pagina Microsoft; finché l'accesso è in corso lo store chiede l'esito al BE a intervalli.
 */
@Injectable({ providedIn: "root" })
export class OneDriveStore {
  private readonly api = inject(OneDriveApi);
  private readonly notifications = inject(NotificationService);

  readonly connection = signal<OneDriveConnection | null>(null);
  readonly isLoading = signal(false);
  /** operazione in corso (avvio, scollegamento, sincronizzazione) */
  readonly busy = signal(false);
  /** true mentre si attende che l'utente autorizzi l'app sulla pagina Microsoft */
  readonly signingIn = signal(false);

  readonly status = computed(() => this.connection()?.status ?? "NotConnected");
  readonly isConnected = computed(() => this.status() === "Connected");

  private timer: ReturnType<typeof setTimeout> | null = null;
  private attempt = 0;

  /** Chiamata dalla pagina all'apertura; restituisce la funzione di disattivazione (ferma i controlli periodici). */
  activate(): () => void {
    void this.load();
    return () => this.stopWaiting();
  }

  async load() {
    this.isLoading.set(true);
    try {
      const connection = await this.api.get();
      this.connection.set(connection);
      // accesso avviato in precedenza e ancora valido (es. pagina ricaricata): si riprende ad attendere
      if (connection.status === "Pending" && connection.userCode) this.waitForSignIn(connection);
    } catch {
      // errore già notificato dal layer API
    } finally {
      this.isLoading.set(false);
    }
  }

  /** Avvia (o ripete) l'accesso a OneDrive. */
  async connect() {
    await this.run(async () => {
      const connection = await this.api.start();
      this.connection.set(connection);
      this.waitForSignIn(connection);
    });
  }

  /** Interrompe l'attesa; un collegamento non ancora completato viene eliminato. */
  async cancel() {
    this.stopWaiting();
    if (this.status() !== "Pending") return;
    await this.run(async () => this.connection.set(await this.api.disconnect()));
  }

  async disconnect() {
    this.stopWaiting();
    await this.run(async () => {
      this.connection.set(await this.api.disconnect());
      this.notifications.showSuccess("OneDrive scollegato");
    });
  }

  async syncNow() {
    await this.run(async () => {
      await this.api.sync();
      this.notifications.showSuccess("Sincronizzazione avviata: i file importati compaiono in Movimenti › Import");
      setTimeout(() => void this.load(), ONEDRIVE_SYNC_REFRESH_MS);
    });
  }

  private async run(action: () => Promise<void>) {
    if (this.busy()) return;
    this.busy.set(true);
    try {
      await action();
    } catch {
      // errore già notificato dal layer API
    } finally {
      this.busy.set(false);
    }
  }

  private waitForSignIn(connection: OneDriveConnection) {
    this.stopWaiting();
    this.signingIn.set(true);
    const attempt = this.attempt;
    const interval = Math.max(connection.pollIntervalSeconds ?? 5, 2) * 1000;
    this.timer = setTimeout(() => void this.checkSignIn(attempt), interval);
  }

  private async checkSignIn(attempt: number) {
    try {
      const connection = await this.api.complete();
      if (attempt !== this.attempt) return;
      this.connection.set(connection);

      if (connection.userCode) {
        this.waitForSignIn(connection);
        return;
      }

      this.stopWaiting();
      if (connection.status === "Connected") this.notifications.showSuccess("OneDrive collegato");
    } catch {
      // accesso rifiutato o codice scaduto (già notificato): si torna allo stato salvato
      if (attempt !== this.attempt) return;
      this.stopWaiting();
      void this.load();
    }
  }

  private stopWaiting() {
    this.attempt++;
    this.signingIn.set(false);
    if (this.timer !== null) {
      clearTimeout(this.timer);
      this.timer = null;
    }
  }
}
