import { PublicClientApplication } from "@azure/msal-browser";
import { msalConfig } from "../config/authConfig";
import { appEnv } from "../config/env";

/**
 * Istanza singleton di MSAL condivisa tra MsalProvider e ApiClient.
 * Deve essere inizializzata prima del render dell'app.
 * È null quando è attivo il bypass di autenticazione per lo sviluppo locale.
 */
export const msalInstance: PublicClientApplication | null = appEnv.devAuthBypass
  ? null
  : new PublicClientApplication(msalConfig);
