import { Box, Stack, Tab, Tabs, Typography } from "@mui/material";
import { type SyntheticEvent, useMemo } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import CategoriesPage from "./CategoriesPage";
import AccountsPage from "./AccountsPage";

type SettingsTab = "categorie" | "account";

const DEFAULT_TAB: SettingsTab = "categorie";

const normalizeTab = (value: string | null): SettingsTab => {
  if (value === "account") {
    return "account";
  }

  return "categorie";
};

export default function SettingsPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const activeTab = useMemo<SettingsTab>(() => {
    return normalizeTab(searchParams.get("tab"));
  }, [searchParams]);

  const handleTabChange = (_event: SyntheticEvent, value: SettingsTab) => {
    const query = value === DEFAULT_TAB ? "" : `?tab=${value}`;
    navigate(`/impostazioni${query}`);
  };

  return (
    <Stack spacing={2.5} sx={{ flex: 1, px: { xs: 2.5, md: 4 }, py: { xs: 2.5, md: 3 } }}>
      <Typography
        variant="h5"
        sx={{
          color: "text.primary",
          fontWeight: 700,
          letterSpacing: "-0.02em",
          px: 1,
        }}
      >
        Impostazioni
      </Typography>

      <Box sx={{ px: { xs: 0.5, md: 1 } }}>
        <Tabs
          value={activeTab}
          onChange={handleTabChange}
          textColor="inherit"
          indicatorColor="primary"
        >
          <Tab value="categorie" label="Categorie" />
          <Tab value="account" label="Account" />
        </Tabs>
      </Box>

      {activeTab === "categorie" ? <CategoriesPage embedded /> : <AccountsPage embedded />}
    </Stack>
  );
}
