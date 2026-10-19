import { StrictMode, ReactNode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import App from "./App.tsx";
import "@fontsource/roboto/300.css";
import "@fontsource/roboto/400.css";
import "@fontsource/roboto/500.css";
import "@fontsource/roboto/700.css";
import { AppThemeProvider } from "./stores/ThemeContext.tsx";
import { ApiErrorProvider } from "./stores/ApiErrorContext.tsx";
import { SuccessMessageProvider } from "./stores/SuccessMessageContext.tsx";
import { MsalProvider } from "@azure/msal-react";
import { msalInstance } from "./auth/msalInstance.ts";

const render = (withAuthProvider: (app: ReactNode) => ReactNode) => {
  createRoot(document.getElementById("root")!).render(
    <StrictMode>
      {withAuthProvider(
        <AppThemeProvider>
          <ApiErrorProvider>
            <SuccessMessageProvider>
              <App />
            </SuccessMessageProvider>
          </ApiErrorProvider>
        </AppThemeProvider>
      )}
    </StrictMode>
  );
};

if (msalInstance) {
  const instance = msalInstance;
  // Inizializza MSAL prima del render per gestire il redirect
  instance.initialize().then(() => {
    render((app) => <MsalProvider instance={instance}>{app}</MsalProvider>);
  });
} else {
  // Bypass di autenticazione per lo sviluppo locale: nessun MsalProvider
  render((app) => app);
}
