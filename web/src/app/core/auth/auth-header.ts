import { PublicClientApplication } from "@azure/msal-browser";
import { apiTokenRequest } from "./authConfig";
import { getDemoAccessToken, hasDemoSession } from "./demoAuth";
import { devAuthorizationHeader, getDevUser } from "./devAuth";
import { msalInstance } from "./msalInstance";

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

/** Header Authorization per le chiamate al BE in base alla modalità di accesso (dev, demo, Entra ID). */
export const getAuthorizationHeader = async (): Promise<string | null> => {
  if (!msalInstance) {
    // Bypass di sviluppo: il BE accetta il nome utente con lo schema DevUser
    const devUser = getDevUser();
    return devUser ? devAuthorizationHeader(devUser) : null;
  }

  if (hasDemoSession()) {
    const demoToken = await getDemoAccessToken();
    return demoToken ? `Bearer ${demoToken}` : null;
  }

  const token = await getAccessToken(msalInstance);
  return token ? `Bearer ${token}` : null;
};
