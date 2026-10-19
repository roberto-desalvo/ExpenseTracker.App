import "./App.css";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import AppLayout from "./components/layout/AppLayout";
import DashboardPage from "./pages/DashboardPage";
import TransactionsPage from "./pages/TransactionsPage";
import AnalyticsPage from "./pages/AnalyticsPage";
import SettingsPage from "./pages/SettingsPage";
import AuthGate from "./auth/AuthGate";
import { AccountsProvider } from "./stores/AccountContext";
import { TransactionsProvider } from "./stores/TransactionContext";
import { CategoriesProvider } from "./stores/CategoryContext";
import { TableContextProvider } from "./stores/TableContext";
import { TransactionModalProvider } from "./stores/TransactionModalContext";
import { QuickActionsProvider } from "./stores/QuickActionsContext";
import { ROUTES } from "./config/routes";

function AppShell() {
  return (
    <AuthGate>
      <AccountsProvider>
        <TransactionsProvider>
          <CategoriesProvider>
            <TableContextProvider>
              <TransactionModalProvider>
                <QuickActionsProvider>
                  <AppLayout>
                    <Routes>
                      <Route path={ROUTES.dashboard} element={<DashboardPage />} />
                      <Route path={ROUTES.transactions} element={<TransactionsPage />} />
                      <Route path={ROUTES.analytics} element={<AnalyticsPage />} />
                      <Route path={ROUTES.settings} element={<SettingsPage />} />
                      <Route path="*" element={<Navigate to={ROUTES.dashboard} replace />} />
                    </Routes>
                  </AppLayout>
                </QuickActionsProvider>
              </TransactionModalProvider>
            </TableContextProvider>
          </CategoriesProvider>
        </TransactionsProvider>
      </AccountsProvider>
    </AuthGate>
  );
}

function App() {
  return (
    <BrowserRouter>
      <AppShell />
    </BrowserRouter>
  );
}

export default App;
