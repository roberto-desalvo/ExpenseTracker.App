import { Box, Stack, Tab, Tabs, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
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
  const theme = useTheme();
  const divider = theme.palette.divider;
  const mode = theme.palette.mode;

  const activeTab = useMemo<SettingsTab>(() => {
    return normalizeTab(searchParams.get("tab"));
  }, [searchParams]);

  const handleTabChange = (_event: SyntheticEvent, value: SettingsTab) => {
    const query = value === DEFAULT_TAB ? "" : `?tab=${value}`;
    navigate(`/impostazioni${query}`);
  };

  const sectionShellSx = {
    border: `1px solid ${divider}`,
    borderRadius: 3,
    backgroundColor: "background.paper",
    boxShadow:
      mode === "dark"
        ? "0 8px 24px rgba(0,0,0,0.2)"
        : "0 8px 20px rgba(15,23,42,0.05)",
    overflow: "hidden",
  };

  return (
    <Stack
      spacing={2}
      sx={{
        flex: 1,
        width: "100%",
        maxWidth: 1480,
        mx: "auto",
        px: { xs: 1.5, md: 3 },
        py: { xs: 2, md: 2.5 },
      }}
    >
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

      <Box sx={sectionShellSx}>
        <Box
          sx={{
            px: { xs: 1.5, md: 2 },
            pt: { xs: 1.25, md: 1.5 },
            borderBottom: `1px solid ${divider}`,
          }}
        >
          <Tabs
            value={activeTab}
            onChange={handleTabChange}
            textColor="inherit"
            indicatorColor="primary"
            sx={{
              minHeight: 40,
              "& .MuiTab-root": {
                minHeight: 40,
                fontSize: "0.88rem",
                fontWeight: 600,
                textTransform: "none",
                letterSpacing: 0,
                px: 1.5,
              },
            }}
          >
            <Tab value="categorie" label="Categorie" />
            <Tab value="account" label="Account" />
          </Tabs>
        </Box>

        <Box sx={{ px: { xs: 0.75, md: 1 }, py: { xs: 1, md: 1.25 } }}>
          {activeTab === "categorie" ? <CategoriesPage embedded /> : <AccountsPage embedded />}
        </Box>
      </Box>
    </Stack>
  );
}
