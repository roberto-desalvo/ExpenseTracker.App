import "./App.css";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import { Box } from "@mui/material";
import HomeHeader from "./components/HomeHeader";
import LandingPage from "./pages/LandingPage";
import TransactionsPage from "./pages/TransactionsPage";
import CategoriesPage from "./pages/CategoriesPage";
import AccountsPage from "./pages/AccountsPage";

function App() {
  return (
    <BrowserRouter>
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
    </BrowserRouter>
  );
}

export default App;
