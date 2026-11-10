import { Injectable, inject } from "@angular/core";
import { apiConfig } from "../config/api";
import { OneDriveConnection } from "../models/OneDriveConnection";
import { ApiClient } from "./api-client";

/** Collegamento del OneDrive personale dell'utente (import automatico degli estratti conto). */
@Injectable({ providedIn: "root" })
export class OneDriveApi {
  private readonly api = inject(ApiClient);

  get(): Promise<OneDriveConnection> {
    return this.api.read("GET", apiConfig.oneDrive.connection, "Errore nel caricamento del collegamento a OneDrive");
  }

  /** Avvia l'accesso: restituisce il codice da inserire sulla pagina Microsoft. */
  start(): Promise<OneDriveConnection> {
    return this.api.send("POST", apiConfig.oneDrive.connection, "Errore nell'avvio del collegamento a OneDrive");
  }

  /** Controlla se l'utente ha autorizzato l'app; in caso positivo completa il collegamento. */
  complete(): Promise<OneDriveConnection> {
    return this.api.send("POST", apiConfig.oneDrive.complete, "Errore nel collegamento a OneDrive");
  }

  disconnect(): Promise<OneDriveConnection> {
    return this.api.send("DELETE", apiConfig.oneDrive.connection, "Errore nello scollegamento di OneDrive");
  }

  /** Accoda una sincronizzazione della cartella. */
  sync(): Promise<void> {
    return this.api.write("POST", apiConfig.oneDrive.sync, "Errore nell'avvio della sincronizzazione");
  }
}
