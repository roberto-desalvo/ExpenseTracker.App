import { Configuration, RedirectRequest, SilentRequest } from "@azure/msal-browser";

const clientId = import.meta.env.VITE_MSAL_CLIENT_ID as string;
const tenantId = import.meta.env.VITE_MSAL_TENANT_ID as string;
const apiScope = import.meta.env.VITE_MSAL_API_SCOPE as string;

export const msalConfig: Configuration = {
  auth: {
    clientId,
    authority: `https://login.microsoftonline.com/${tenantId}`,
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: "sessionStorage",
    storeAuthStateInCookie: false,
  },
};

/** Scopes usati per il login interattivo */
export const loginRequest: RedirectRequest = {
  scopes: [apiScope],
};

/** Scopes usati per acquisire il token in silenzio per le API */
export const apiTokenRequest = (account: object): SilentRequest => ({
  scopes: [apiScope],
  account: account as SilentRequest["account"],
});
