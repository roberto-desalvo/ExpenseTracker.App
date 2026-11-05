import { HttpClient, HttpContext } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { firstValueFrom } from "rxjs";
import { ERROR_FALLBACK } from "./interceptors";

/**
 * Accesso al BE: autenticazione ed errori sono gestiti dagli interceptor (`interceptors.ts`).
 * Le letture identiche ancora in volo (metodo + url + body) condividono la stessa promise,
 * così più consumer che atterrano insieme generano una sola chiamata HTTP.
 */
@Injectable({ providedIn: "root" })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly inFlight = new Map<string, Promise<unknown>>();

  /** Lettura (GET, o POST di query) con deduplica delle richieste concorrenti. */
  read<T>(method: "GET" | "POST", url: string, fallbackMessage: string, body?: unknown): Promise<T> {
    const key = `${method} ${url} ${body === undefined ? "" : JSON.stringify(body)}`;
    const pending = this.inFlight.get(key);
    if (pending) {
      return pending as Promise<T>;
    }

    const request = firstValueFrom(
      this.http.request<T>(method, url, { body, context: this.context(fallbackMessage) }),
    ).finally(() => this.inFlight.delete(key));
    this.inFlight.set(key, request);
    return request;
  }

  /** Scrittura (POST/PUT/DELETE) senza corpo di risposta atteso. */
  async write(method: "POST" | "PUT" | "DELETE", url: string, fallbackMessage: string, body?: unknown): Promise<void> {
    await firstValueFrom(
      this.http.request(method, url, {
        body,
        responseType: "text",
        context: this.context(fallbackMessage),
      }),
    );
  }

  /** Upload multipart (il browser imposta content type e boundary del FormData), con corpo di risposta JSON. */
  upload<T>(url: string, fallbackMessage: string, form: FormData): Promise<T> {
    return firstValueFrom(this.http.post<T>(url, form, { context: this.context(fallbackMessage) }));
  }

  private context(fallbackMessage: string) {
    return new HttpContext().set(ERROR_FALLBACK, fallbackMessage);
  }
}
