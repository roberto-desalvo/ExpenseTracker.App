import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import App from "./App.tsx";
import "@fontsource/roboto/300.css";
import "@fontsource/roboto/400.css";
import "@fontsource/roboto/500.css";
import "@fontsource/roboto/700.css";
import { TransactionsProvider } from "./stores/TransactionContext.tsx";
import { AccountsProvider } from "./stores/AccountContext.tsx";
import { TableContextProvider } from "./stores/TableContext.tsx";
import { TransactionModalProvider } from "./stores/TransactionModalContext.tsx";
import { CategoriesProvider } from "./stores/CategoryContext.tsx";
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
              <AccountsProvider>
                <TransactionsProvider>
                  <CategoriesProvider>
                    <TableContextProvider>
                      <TransactionModalProvider>
                        <App />
                      </TransactionModalProvider>
                    </TableContextProvider>
                  </CategoriesProvider>
                </TransactionsProvider>
              </AccountsProvider>
            </SuccessMessageProvider>
          </ApiErrorProvider>
        </AppThemeProvider>
      </MsalProvider>
    </StrictMode>
  );
});
