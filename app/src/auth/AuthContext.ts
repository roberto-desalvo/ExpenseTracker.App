import { createContext, useContext } from "react";

export interface AuthState {
  isAuthenticated: boolean;
  userName: string;
  logout: () => void;
}

/** Stato di autenticazione indipendente dal provider (MSAL o login di sviluppo). */
export const AuthContext = createContext<AuthState | null>(null);

export const useAuth = (): AuthState => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthGate");
  }
  return context;
};
