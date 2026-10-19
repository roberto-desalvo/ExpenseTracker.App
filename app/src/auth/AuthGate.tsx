import { ReactNode, useCallback, useEffect, useMemo, useState } from "react";
import { Box, CircularProgress } from "@mui/material";
import { MsalAuthenticationTemplate, useIsAuthenticated, useMsal } from "@azure/msal-react";
import { EventMessage, EventType, InteractionStatus, InteractionType } from "@azure/msal-browser";
import { loginRequest } from "../config/authConfig";
import { appEnv } from "../config/env";
import AuthErrorPage from "../pages/AuthErrorPage";
import DevLoginPage from "../pages/DevLoginPage";
import LoginPage from "../pages/LoginPage";
import { AuthContext } from "./AuthContext";
import { clearDemoSession, getDemoUserName, hasDemoSession, startDemoSession } from "./demoAuth";
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

function DemoAuthBridge({ children, onLogout }: AuthGateProps & { onLogout: () => void }) {
  const value = useMemo(
    () => ({
      isAuthenticated: true,
      userName: getDemoUserName(),
      logout: onLogout,
    }),
    [onLogout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/**
 * Produzione: se c'è già una sessione (demo o MSAL) la riprende, altrimenti mostra la scelta
 * tra login Entra ID e avvio della demo con l'utente di test.
 */
function ProductionAuthGate({ children }: AuthGateProps) {
  const { instance, inProgress } = useMsal();
  const isMsalAuthenticated = useIsAuthenticated();
  const [demoActive, setDemoActive] = useState(hasDemoSession);
  const [loginError, setLoginError] = useState<string | null>(null);

  useEffect(() => {
    const callbackId = instance.addEventCallback((event: EventMessage) => {
      // In MSAL v5 anche il login via redirect fallito viene notificato come ACQUIRE_TOKEN_FAILURE.
      if (event.eventType === EventType.ACQUIRE_TOKEN_FAILURE && event.error) {
        setLoginError(event.error.message);
      }
    });
    return () => {
      if (callbackId) instance.removeEventCallback(callbackId);
    };
  }, [instance]);

  const endDemo = useCallback(() => {
    clearDemoSession();
    setDemoActive(false);
  }, []);

  if (demoActive) {
    return <DemoAuthBridge onLogout={endDemo}>{children}</DemoAuthBridge>;
  }

  if (isMsalAuthenticated) {
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

  // Ritorno dal redirect di Entra ID in corso.
  if (inProgress !== InteractionStatus.None) {
    return (
      <Box sx={{ minHeight: "100vh", display: "grid", placeItems: "center" }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <LoginPage
      error={loginError}
      onEntraLogin={() => {
        setLoginError(null);
        instance.loginRedirect(loginRequest).catch((error: unknown) => {
          setLoginError(error instanceof Error ? error.message : "Accesso non riuscito.");
        });
      }}
      onDemoLogin={
        appEnv.demoLoginEnabled
          ? async () => {
              await startDemoSession();
              setDemoActive(true);
            }
          : undefined
      }
    />
  );
}

/**
 * Richiede un utente autenticato prima di renderizzare i figli:
 * login Entra ID via MSAL o demo con l'utente di test, oppure solo nome utente in sviluppo locale (bypass).
 */
export default function AuthGate({ children }: AuthGateProps) {
  if (appEnv.devAuthBypass) {
    return <DevAuthGate>{children}</DevAuthGate>;
  }

  return <ProductionAuthGate>{children}</ProductionAuthGate>;
}
