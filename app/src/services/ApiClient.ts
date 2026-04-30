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

export const apiFetchJson = async <T>(
  url: string,
  init: RequestInit,
  fallbackMessage: string
): Promise<T> => {
  try {
    const response = await fetch(url, init);

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
    const response = await fetch(url, init);

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
