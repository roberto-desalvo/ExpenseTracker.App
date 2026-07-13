import "./App.css";
import { BrowserRouter, Route, Routes, useLocation } from "react-router-dom";
import { Box } from "@mui/material";
import HomeHeader from "./components/HomeHeader";
import LandingPage from "./pages/LandingPage";
import SettingsPage from "./pages/SettingsPage";
import { MsalAuthenticationTemplate } from "@azure/msal-react";
import { InteractionType } from "@azure/msal-browser";
import { loginRequest } from "./config/authConfig";
import AuthErrorPage from "./pages/AuthErrorPage";
import { AccountsProvider } from "./stores/AccountContext";
import { TransactionsProvider } from "./stores/TransactionContext";
import { CategoriesProvider } from "./stores/CategoryContext";
import { TableContextProvider } from "./stores/TableContext";
import { TransactionModalProvider } from "./stores/TransactionModalContext";

function AppShell() {
  const location = useLocation();
  const isHomeRoute = location.pathname === "/";

  return (
    <MsalAuthenticationTemplate
      interactionType={InteractionType.Redirect}
      authenticationRequest={loginRequest}
      errorComponent={AuthErrorPage}
    >
      <AccountsProvider>
        <TransactionsProvider>
          <CategoriesProvider>
            <TableContextProvider>
              <TransactionModalProvider>
                <Box
                  sx={{
                    display: "flex",
                    flexDirection: "column",
                    minHeight: "100vh",
                    width: "100%",
                    bgcolor: "background.default",
                    overflowX: "hidden",
                  }}
                >
                  <HomeHeader sticky={isHomeRoute} />
                  <Routes>
                    <Route path="/" element={<LandingPage />} />
                    <Route path="/impostazioni" element={<SettingsPage />} />
                  </Routes>
                </Box>
              </TransactionModalProvider>
            </TableContextProvider>
          </CategoriesProvider>
        </TransactionsProvider>
      </AccountsProvider>
    </MsalAuthenticationTemplate>
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
