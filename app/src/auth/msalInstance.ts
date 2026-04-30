import { PublicClientApplication } from "@azure/msal-browser";
import { msalConfig } from "../config/authConfig";

/**
 * Istanza singleton di MSAL condivisa tra MsalProvider e ApiClient.
 * Deve essere inizializzata prima del render dell'app.
 */
export const msalInstance = new PublicClientApplication(msalConfig);
