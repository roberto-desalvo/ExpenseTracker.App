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

function App() {
  return (
    <BrowserRouter>
      <MsalAuthenticationTemplate
        interactionType={InteractionType.Redirect}
        authenticationRequest={loginRequest}
      >
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
      </MsalAuthenticationTemplate>
    </BrowserRouter>
  );
}

export default App;
