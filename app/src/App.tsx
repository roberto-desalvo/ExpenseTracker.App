import "./App.css";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import { Box } from "@mui/material";
import HomeHeader from "./components/HomeHeader";
import LandingPage from "./pages/LandingPage";
import TransactionsPage from "./pages/TransactionsPage";
import CategoriesPage from "./pages/CategoriesPage";
import AccountsPage from "./pages/AccountsPage";
import { MsalAuthenticationTemplate } from "@azure/msal-react";
import { InteractionType } from "@azure/msal-browser";
import { loginRequest } from "./config/authConfig";
import AuthErrorPage from "./pages/AuthErrorPage";
import { AccountsProvider } from "./stores/AccountContext";
import { TransactionsProvider } from "./stores/TransactionContext";
import { CategoriesProvider } from "./stores/CategoryContext";
import { TableContextProvider } from "./stores/TableContext";
import { TransactionModalProvider } from "./stores/TransactionModalContext";

function App() {
  return (
    <BrowserRouter>
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
                    <HomeHeader />
                    <Routes>
                      <Route path="/" element={<LandingPage />} />
                      <Route path="/transazioni" element={<TransactionsPage />} />
                      <Route path="/categorie" element={<CategoriesPage />} />
                      <Route path="/account" element={<AccountsPage />} />
                    </Routes>
                  </Box>
                </TransactionModalProvider>
              </TableContextProvider>
            </CategoriesProvider>
          </TransactionsProvider>
        </AccountsProvider>
      </MsalAuthenticationTemplate>
    </BrowserRouter>
  );
}

export default App;
