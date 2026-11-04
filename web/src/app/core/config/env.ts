import { rawEnv } from "../../../environments/env.generated";

type EnvKey =
  | "VITE_EXPENSE_TRACKER_API_BASE_URL"
  | "VITE_MSAL_CLIENT_ID"
  | "VITE_MSAL_TENANT_ID"
  | "VITE_MSAL_API_SCOPE"
  | "VITE_DEV_AUTH_BYPASS"
  | "VITE_DEMO_LOGIN_ENABLED";

const env = rawEnv as { DEV: boolean } & Partial<Record<EnvKey, string>>;

const requireEnv = (key: EnvKey) => {
  const value = env[key];

  if (!value || value.trim().length === 0) {
    throw new Error(`Missing required environment variable: ${key}`);
  }

  return value;
};

// Senza schema il browser tratta l'URL come relativo e lo accoda all'origine del sito.
const normalizeBaseUrl = (value: string) => {
  const trimmed = value.trim().replace(/\/+$/, "");
  if (!/^https?:\/\//i.test(trimmed)) {
    throw new Error(`VITE_EXPENSE_TRACKER_API_BASE_URL must be an absolute URL (http:// or https://), got "${trimmed}"`);
  }
  return trimmed;
};

/**
 * Bypass dell'autenticazione Entra ID: attivo solo con `npm start` (mai nelle build),
 * disattivabile con VITE_DEV_AUTH_BYPASS=false per provare MSAL in locale.
 */
const devAuthBypass = env.DEV && env.VITE_DEV_AUTH_BYPASS !== "false";

// Con il bypass attivo le variabili MSAL non vengono usate e possono mancare.
const msalEnv = (key: EnvKey) => (devAuthBypass ? "" : requireEnv(key));

/** Pulsante "Prova la demo" nella pagina di login (richiede DemoAuth abilitato sul BE). Attivo salvo VITE_DEMO_LOGIN_ENABLED=false. */
const demoLoginEnabled = env.VITE_DEMO_LOGIN_ENABLED !== "false";

export const appEnv = {
  apiBaseUrl: normalizeBaseUrl(requireEnv("VITE_EXPENSE_TRACKER_API_BASE_URL")),
  devAuthBypass,
  demoLoginEnabled,
  msalClientId: msalEnv("VITE_MSAL_CLIENT_ID"),
  msalTenantId: msalEnv("VITE_MSAL_TENANT_ID"),
  msalApiScope: msalEnv("VITE_MSAL_API_SCOPE"),
} as const;
