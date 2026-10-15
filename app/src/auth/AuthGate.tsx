import { ReactNode, useMemo, useState } from "react";
import { MsalAuthenticationTemplate, useIsAuthenticated, useMsal } from "@azure/msal-react";
import { InteractionType } from "@azure/msal-browser";
import { loginRequest } from "../config/authConfig";
import { appEnv } from "../config/env";
import AuthErrorPage from "../pages/AuthErrorPage";
import DevLoginPage from "../pages/DevLoginPage";
import { AuthContext } from "./AuthContext";
import { clearDevUser, getDevUser, setDevUser } from "./devAuth";

interface AuthGateProps {
  children: ReactNode;
}

function MsalAuthBridge({ children }: AuthGateProps) {
  const { instance, accounts } = useMsal();
  const isAuthenticated = useIsAuthenticated();

  const value = useMemo(
    () => ({
      isAuthenticated,
      userName: accounts[0]?.name ?? accounts[0]?.username ?? "",
      logout: () => {
        instance.logoutRedirect();
      },
    }),
    [instance, accounts, isAuthenticated]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

function DevAuthGate({ children }: AuthGateProps) {
  const [userName, setUserName] = useState<string | null>(getDevUser);

  const value = useMemo(
    () => ({
      isAuthenticated: true,
      userName: userName ?? "",
      logout: () => {
        clearDevUser();
        setUserName(null);
      },
    }),
    [userName]
  );

  if (!userName) {
    return (
      <DevLoginPage
        onLogin={(name) => {
          setDevUser(name);
          setUserName(name);
        }}
      />
    );
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/**
 * Richiede un utente autenticato prima di renderizzare i figli:
 * login Entra ID via MSAL, oppure solo nome utente in sviluppo locale (bypass).
 */
export default function AuthGate({ children }: AuthGateProps) {
  if (appEnv.devAuthBypass) {
    return <DevAuthGate>{children}</DevAuthGate>;
  }

  return (
    <MsalAuthenticationTemplate
      interactionType={InteractionType.Redirect}
      authenticationRequest={loginRequest}
      errorComponent={AuthErrorPage}
    >
      <MsalAuthBridge>{children}</MsalAuthBridge>
    </MsalAuthenticationTemplate>
  );
}
