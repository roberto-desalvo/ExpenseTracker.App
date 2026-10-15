const requireEnv = (key: keyof ImportMetaEnv) => {
  const value = import.meta.env[key];

  if (!value || value.trim().length === 0) {
    throw new Error(`Missing required environment variable: ${key}`);
  }

  return value;
};

const normalizeBaseUrl = (value: string) => value.replace(/\/+$/, "");

/**
 * Bypass dell'autenticazione Entra ID: attivo solo con `npm run dev` (mai nelle build),
 * disattivabile con VITE_DEV_AUTH_BYPASS=false per provare MSAL in locale.
 */
const devAuthBypass = import.meta.env.DEV && import.meta.env.VITE_DEV_AUTH_BYPASS !== "false";

// Con il bypass attivo le variabili MSAL non vengono usate e possono mancare.
const msalEnv = (key: keyof ImportMetaEnv) => (devAuthBypass ? "" : requireEnv(key));

export const appEnv = {
  apiBaseUrl: normalizeBaseUrl(requireEnv("VITE_EXPENSE_TRACKER_API_BASE_URL")),
  devAuthBypass,
  msalClientId: msalEnv("VITE_MSAL_CLIENT_ID"),
  msalTenantId: msalEnv("VITE_MSAL_TENANT_ID"),
  msalApiScope: msalEnv("VITE_MSAL_API_SCOPE"),
} as const;
