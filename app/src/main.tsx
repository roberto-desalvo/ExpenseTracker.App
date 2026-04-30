import { StrictMode } from "react";
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

// Inizializza MSAL prima del render per gestire il redirect
msalInstance.initialize().then(() => {
  createRoot(document.getElementById("root")!).render(
    <StrictMode>
      <MsalProvider instance={msalInstance}>
        <AppThemeProvider>
          <ApiErrorProvider>
            <SuccessMessageProvider>
              <App />
            </SuccessMessageProvider>
          </ApiErrorProvider>
        </AppThemeProvider>
      </MsalProvider>
    </StrictMode>
  );
});

