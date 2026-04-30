const requireEnv = (key: keyof ImportMetaEnv) => {
  const value = import.meta.env[key];

  if (!value || value.trim().length === 0) {
    throw new Error(`Missing required environment variable: ${key}`);
  }

  return value;
};

const normalizeBaseUrl = (value: string) => value.replace(/\/+$/, "");

export const appEnv = {
  apiBaseUrl: normalizeBaseUrl(requireEnv("VITE_EXPENSE_TRACKER_API_BASE_URL")),
  msalClientId: requireEnv("VITE_MSAL_CLIENT_ID"),
  msalTenantId: requireEnv("VITE_MSAL_TENANT_ID"),
  msalApiScope: requireEnv("VITE_MSAL_API_SCOPE"),
} as const;
