import { ReactNode, useMemo } from "react";
import { Box, ButtonBase, Stack, Tab, Tabs, Typography, useMediaQuery } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { AccountBalanceWalletOutlined, CategoryOutlined, PaletteOutlined } from "@mui/icons-material";
import { useNavigate, useSearchParams } from "react-router-dom";
import CategoriesPage from "./CategoriesPage";
import AccountsPage from "./AccountsPage";
import AppearanceSettings from "../components/settings/AppearanceSettings";
import PageContainer from "../components/ui/PageContainer";
import PageHeader from "../components/ui/PageHeader";
import { ROUTES } from "../config/routes";

type SettingsSection = "categorie" | "conti" | "aspetto";

const SECTIONS: { id: SettingsSection; label: string; description: string; icon: ReactNode }[] = [
  {
    id: "categorie",
    label: "Categorie",
    description: "Classificazione dei movimenti e regole di import",
    icon: <CategoryOutlined fontSize="small" />,
  },
  {
    id: "conti",
    label: "Conti",
    description: "Conti correnti, carte e wallet",
    icon: <AccountBalanceWalletOutlined fontSize="small" />,
  },
  {
    id: "aspetto",
    label: "Aspetto",
    description: "Stile, tema e navigazione",
    icon: <PaletteOutlined fontSize="small" />,
  },
];

/** Accetta anche il vecchio parametro `?tab=account|categorie`. */
const normalizeSection = (params: URLSearchParams): SettingsSection => {
  const value = params.get("sezione") ?? params.get("tab");
  if (value === "conti" || value === "account") return "conti";
  if (value === "aspetto") return "aspetto";
  return "categorie";
};

export default function SettingsPage() {
  const theme = useTheme();
  const a = theme.app;
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const isDesktop = useMediaQuery(theme.breakpoints.up("md"));

  const active = useMemo(() => normalizeSection(searchParams), [searchParams]);
  const current = SECTIONS.find((s) => s.id === active)!;

  const go = (section: SettingsSection) =>
    navigate(section === "categorie" ? ROUTES.settings : `${ROUTES.settings}?sezione=${section}`);

  return (
    <PageContainer>
      <PageHeader title="Impostazioni" subtitle="Gestisci le anagrafiche e personalizza l'aspetto dell'app." />

      {!isDesktop && (
        <Tabs
          value={active}
          onChange={(_e, value: SettingsSection) => go(value)}
          variant="fullWidth"
          sx={{ borderBottom: `1px solid ${a.border}`, "& .MuiTab-root": { minHeight: 48 } }}
        >
          {SECTIONS.map((s) => (
            <Tab key={s.id} value={s.id} label={s.label} icon={s.icon as React.ReactElement} iconPosition="start" />
          ))}
        </Tabs>
      )}

      <Stack direction="row" spacing={3} alignItems="flex-start">
        {isDesktop && (
          <Stack component="nav" aria-label="Sezioni impostazioni" spacing={0.5} sx={{ width: 260, flexShrink: 0, position: "sticky", top: 24 }}>
            {SECTIONS.map((s) => {
              const selected = s.id === active;
              return (
                <ButtonBase
                  key={s.id}
                  onClick={() => go(s.id)}
                  aria-current={selected ? "page" : undefined}
                  sx={{
                    justifyContent: "flex-start",
                    textAlign: "left",
                    gap: 1.5,
                    p: 1.5,
                    borderRadius: `${a.radiusControl + 2}px`,
                    border: `1px solid ${selected ? a.border : "transparent"}`,
                    backgroundColor: selected ? a.surface : "transparent",
                    boxShadow: selected ? a.cardShadow : "none",
                    "&:hover": { backgroundColor: selected ? a.surface : a.surfaceAlt },
                  }}
                >
                  <Box
                    sx={{
                      width: 34,
                      height: 34,
                      flexShrink: 0,
                      borderRadius: `${Math.max(a.radiusControl, 6)}px`,
                      display: "grid",
                      placeItems: "center",
                      backgroundColor: selected ? a.primarySoft : a.surfaceAlt,
                      color: selected ? a.primary : a.textSecondary,
                    }}
                  >
                    {s.icon}
                  </Box>
                  <Box sx={{ minWidth: 0 }}>
                    <Typography sx={{ fontWeight: 650, fontSize: "0.9rem", color: a.text }}>{s.label}</Typography>
                    <Typography sx={{ fontSize: "0.74rem", color: a.textSecondary, lineHeight: 1.35 }}>{s.description}</Typography>
                  </Box>
                </ButtonBase>
              );
            })}
          </Stack>
        )}

        <Box sx={{ flex: 1, minWidth: 0 }}>
          {isDesktop && (
            <Box sx={{ mb: 2 }}>
              <Typography component="h2" sx={{ fontWeight: 700, fontSize: "1.15rem" }}>
                {current.label}
              </Typography>
              <Typography sx={{ fontSize: "0.84rem", color: a.textSecondary }}>{current.description}</Typography>
            </Box>
          )}
          {active === "categorie" && <CategoriesPage />}
          {active === "conti" && <AccountsPage />}
          {active === "aspetto" && <AppearanceSettings />}
        </Box>
      </Stack>
    </PageContainer>
  );
}
