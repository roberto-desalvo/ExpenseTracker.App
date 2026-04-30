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

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <AppThemeProvider>
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
    </AppThemeProvider>
  </StrictMode>
);
