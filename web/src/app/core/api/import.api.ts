import { Injectable, inject } from "@angular/core";
import { apiConfig } from "../config/api";
import { ImportLog, ImportLogQueryRequest } from "../models/ImportLog";
import { PagedResult } from "../models/PagedResult";
import { ApiClient } from "./api-client";

@Injectable({ providedIn: "root" })
export class ImportApi {
  private readonly api = inject(ApiClient);

  /** Carica il file e lo mette in coda: restituisce l'import registrato (stato Queued). */
  upload(accountId: number, file: File): Promise<ImportLog> {
    const form = new FormData();
    form.append("accountId", String(accountId));
    form.append("file", file, file.name);
    return this.api.upload(apiConfig.imports.base, "Errore nel caricamento del file", form);
  }

  query(request: ImportLogQueryRequest): Promise<PagedResult<ImportLog>> {
    return this.api.read("POST", apiConfig.imports.query, "Errore nel caricamento degli import", request);
  }
}
