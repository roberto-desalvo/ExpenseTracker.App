import { Configuration, RedirectRequest, SilentRequest } from "@azure/msal-browser";
import { appEnv } from "./env";

export const msalConfig: Configuration = {
  auth: {
    clientId: appEnv.msalClientId,
    authority: `https://login.microsoftonline.com/${appEnv.msalTenantId}`,
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
  scopes: [appEnv.msalApiScope],
};

/** Scopes usati per acquisire il token in silenzio per le API */
export const apiTokenRequest = (account: object): SilentRequest => ({
  scopes: [appEnv.msalApiScope],
  account: account as SilentRequest["account"],
});
