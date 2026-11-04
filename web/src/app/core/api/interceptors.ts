import {
  HttpContextToken,
  HttpErrorResponse,
  HttpInterceptorFn,
} from "@angular/common/http";
import { inject } from "@angular/core";
import { catchError, from, switchMap, throwError } from "rxjs";
import { getAuthorizationHeader } from "../auth/auth-header";
import { appEnv } from "../config/env";
import { NotificationService } from "./notification.service";

/** Messaggio mostrato quando la risposta d'errore non ne contiene uno. */
export const ERROR_FALLBACK = new HttpContextToken<string>(() => "Si è verificato un errore");

export class ApiRequestError extends Error {
  constructor(
    message: string,
    public readonly status: number,
  ) {
    super(message);
  }
}

/** Aggiunge l'header Authorization (DevUser, token demo o token Entra ID) alle chiamate verso il BE. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(appEnv.apiBaseUrl)) {
    return next(req);
  }

  return from(getAuthorizationHeader()).pipe(
    switchMap((authorization) =>
      next(authorization ? req.clone({ setHeaders: { Authorization: authorization } }) : req),
    ),
  );
};

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

/** Estrae il messaggio dai problem details del BE (detail → title → primo errore di validazione) o dal testo grezzo. */
export const extractErrorMessage = (body: unknown): string | null => {
  if (body == null || body === "") {
    return null;
  }

  if (typeof body === "string") {
    try {
      return extractErrorMessage(JSON.parse(body) as ProblemDetails) ?? body;
    } catch {
      return body;
    }
  }

  if (typeof body === "object") {
    const json = body as ProblemDetails;
    if (json.detail) return json.detail;
    if (json.title) return json.title;
    if (json.errors) {
      const firstError = Object.values(json.errors).flat()[0];
      if (firstError) return firstError;
    }
  }

  return null;
};

/** Converte ogni errore HTTP in un messaggio leggibile, lo notifica e lo rilancia come ApiRequestError. */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(NotificationService);
  const fallback = req.context.get(ERROR_FALLBACK);

  return next(req).pipe(
    catchError((error: unknown) => {
      let message = fallback;
      let status = 0;

      if (error instanceof HttpErrorResponse) {
        status = error.status;
        if (error.status === 0) {
          message = fallback;
        } else {
          const extracted = extractErrorMessage(error.error);
          message = extracted && extracted.trim().length > 0 ? extracted : `${fallback} (HTTP ${error.status})`;
        }
      } else if (error instanceof Error && error.message) {
        message = error.message;
      }

      notifications.showError(message);
      return throwError(() => new ApiRequestError(message, status));
    }),
  );
};
