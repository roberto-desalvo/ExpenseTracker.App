import { PublicClientApplication } from "@azure/msal-browser";
import { msalInstance } from "../auth/msalInstance";
import { devAuthorizationHeader, getDevUser } from "../auth/devAuth";
import { apiTokenRequest } from "../config/authConfig";

export const API_ERROR_EVENT = "expense-tracker:api-error";

type ApiErrorEventDetail = {
  message: string;
};

class ApiRequestError extends Error {
  public readonly alreadyNotified: boolean;

  constructor(message: string, alreadyNotified = false) {
    super(message);
    this.alreadyNotified = alreadyNotified;
  }
}

const emitApiError = (message: string) => {
  window.dispatchEvent(
    new CustomEvent<ApiErrorEventDetail>(API_ERROR_EVENT, {
      detail: { message },
    })
  );
};

/**
 * Tenta di acquisire silenziosamente un access token per le API.
 * Se non ci sono account loggati restituisce null.
 */
const getAccessToken = async (instance: PublicClientApplication): Promise<string | null> => {
  const account = instance.getActiveAccount() ?? instance.getAllAccounts()[0];
  if (!account) return null;
  try {
    const result = await instance.acquireTokenSilent(apiTokenRequest(account));
    return result.accessToken;
  } catch {
    // Token silenzioso fallito: forza redirect
    await instance.acquireTokenRedirect(apiTokenRequest(account));
    return null;
  }
};

const getAuthorizationHeader = async (): Promise<string | null> => {
  if (!msalInstance) {
    // Bypass di sviluppo: il BE accetta il nome utente con lo schema DevUser
    const devUser = getDevUser();
    return devUser ? devAuthorizationHeader(devUser) : null;
  }

  const token = await getAccessToken(msalInstance);
  return token ? `Bearer ${token}` : null;
};

const withAuthHeader = async (init: RequestInit): Promise<RequestInit> => {
  const authorization = await getAuthorizationHeader();
  if (!authorization) return init;
  return {
    ...init,
    headers: {
      ...(init.headers ?? {}),
      Authorization: authorization,
    },
  };
};

const tryExtractErrorMessage = async (response: Response): Promise<string | null> => {
  try {
    const text = await response.text();

    if (!text) {
      return null;
    }

    try {
      const json = JSON.parse(text) as {
        title?: string;
        detail?: string;
        errors?: Record<string, string[]>;
      };

      if (json.detail) {
        return json.detail;
      }

      if (json.title) {
        return json.title;
      }

      if (json.errors) {
        const firstError = Object.values(json.errors).flat()[0];
        if (firstError) {
          return firstError;
        }
      }
    } catch {
      return text;
    }

    return text;
  } catch {
    return null;
  }
};

const buildErrorMessage = async (response: Response, fallbackMessage: string) => {
  const extracted = await tryExtractErrorMessage(response);

  if (extracted && extracted.trim().length > 0) {
    return extracted;
  }

  return `${fallbackMessage} (HTTP ${response.status})`;
};

/** Letture in corso, indicizzate per metodo + url + body: richieste identiche e concorrenti condividono la stessa promise. */
const inFlightRequests = new Map<string, Promise<unknown>>();

const buildRequestKey = (url: string, init: RequestInit): string | null => {
  if (init.body != null && typeof init.body !== "string") {
    return null;
  }
  return `${init.method ?? "GET"} ${url} ${init.body ?? ""}`;
};

/**
 * Tutte le chiamate che passano di qui sono letture (le scritture usano `apiFetchVoid`):
 * le richieste identiche ancora in volo vengono deduplicate, così il doppio effect di
 * StrictMode o più consumer che atterrano insieme generano una sola chiamata HTTP.
 */
export const apiFetchJson = <T>(
  url: string,
  init: RequestInit,
  fallbackMessage: string
): Promise<T> => {
  const key = buildRequestKey(url, init);
  if (!key) {
    return executeFetchJson<T>(url, init, fallbackMessage);
  }

  const pending = inFlightRequests.get(key);
  if (pending) {
    return pending as Promise<T>;
  }

  const request = executeFetchJson<T>(url, init, fallbackMessage).finally(() => {
    inFlightRequests.delete(key);
  });
  inFlightRequests.set(key, request);
  return request;
};

const executeFetchJson = async <T>(
  url: string,
  init: RequestInit,
  fallbackMessage: string
): Promise<T> => {
  try {
    const authInit = await withAuthHeader(init);
    const response = await fetch(url, authInit);

    if (!response.ok) {
      const message = await buildErrorMessage(response, fallbackMessage);
      emitApiError(message);
      throw new ApiRequestError(message, true);
    }

    return response.json() as Promise<T>;
  } catch (error) {
    if (error instanceof ApiRequestError && error.alreadyNotified) {
      throw error;
    }

    if (error instanceof Error) {
      emitApiError(error.message || fallbackMessage);
      throw error;
    }

    emitApiError(fallbackMessage);
    throw new Error(fallbackMessage);
  }
};

export const apiFetchVoid = async (
  url: string,
  init: RequestInit,
  fallbackMessage: string
): Promise<void> => {
  try {
    const authInit = await withAuthHeader(init);
    const response = await fetch(url, authInit);

    if (!response.ok) {
      const message = await buildErrorMessage(response, fallbackMessage);
      emitApiError(message);
      throw new ApiRequestError(message, true);
    }
  } catch (error) {
    if (error instanceof ApiRequestError && error.alreadyNotified) {
      throw error;
    }

    if (error instanceof Error) {
      emitApiError(error.message || fallbackMessage);
      throw error;
    }

    emitApiError(fallbackMessage);
    throw new Error(fallbackMessage);
  }
};
