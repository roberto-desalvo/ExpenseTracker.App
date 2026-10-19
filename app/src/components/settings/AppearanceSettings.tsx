import { ReactNode } from "react";
import { Box, ButtonBase, Stack, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import {
  CheckCircleRounded,
  DarkModeOutlined,
  LightModeOutlined,
  SettingsBrightnessOutlined,
} from "@mui/icons-material";
import { NavLayout, ThemeModePreference, useAppTheme } from "../../stores/ThemeContext";
import { STYLE_PRESET_LIST, StylePreset } from "../../theme/stylePresets";
import SurfaceCard from "../ui/SurfaceCard";

function OptionCard({
  selected,
  onClick,
  children,
  label,
}: {
  selected: boolean;
  onClick: () => void;
  children: ReactNode;
  label: string;
}) {
  const a = useTheme().app;
  return (
    <ButtonBase
      onClick={onClick}
      aria-pressed={selected}
      aria-label={label}
      sx={{
        position: "relative",
        display: "block",
        textAlign: "left",
        width: "100%",
        borderRadius: `${a.radiusCard}px`,
        border: `1px solid ${selected ? a.primary : a.border}`,
        boxShadow: selected ? `0 0 0 1px ${a.primary}` : "none",
        backgroundColor: a.surface,
        overflow: "hidden",
        transition: "border-color .15s ease, box-shadow .15s ease, transform .15s ease",
        "&:hover": { borderColor: selected ? a.primary : a.borderStrong, transform: "translateY(-1px)" },
      }}
    >
      {selected && (
        <CheckCircleRounded
          sx={{ position: "absolute", top: 10, right: 10, color: a.primary, fontSize: 22, zIndex: 1, backgroundColor: a.surface, borderRadius: "50%" }}
        />
      )}
      {children}
    </ButtonBase>
  );
}

/** Mini dashboard disegnata con i token dello stile, nella modalità corrente. */
function PresetPreview({ preset }: { preset: StylePreset }) {
  const { mode } = useAppTheme();
  const t = preset[mode];
  const r = preset.radiusCard / 2.2;

  return (
    <Box sx={{ backgroundColor: t.background, p: 1.5, height: 150, display: "flex", gap: 1 }}>
      <Box sx={{ width: 34, borderRadius: `${r}px`, backgroundColor: t.sidebarBackground, border: `1px solid ${t.border}`, p: 0.75 }}>
        {[0, 1, 2, 3].map((i) => (
          <Box
            key={i}
            sx={{ height: 6, borderRadius: 2, mb: 0.75, backgroundColor: i === 0 ? t.primary : t.border }}
          />
        ))}
      </Box>
      <Box sx={{ flex: 1, display: "grid", gap: 1, gridTemplateColumns: "1.3fr 1fr", gridTemplateRows: "1fr 1fr" }}>
        <Box
          sx={{
            gridRow: "span 2",
            borderRadius: `${r}px`,
            background: t.hero.background,
            border: `1px solid ${t.hero.border}`,
            p: 1,
            display: "flex",
            flexDirection: "column",
            justifyContent: "space-between",
          }}
        >
          <Box>
            <Box sx={{ width: "45%", height: 4, borderRadius: 2, backgroundColor: t.hero.subtle, mb: 0.75 }} />
            <Typography sx={{ fontFamily: preset.headingFontFamily, color: t.hero.text, fontWeight: 750, fontSize: "0.95rem", lineHeight: 1 }}>
              €24.180
            </Typography>
          </Box>
          <svg viewBox="0 0 100 30" width="100%" height="30" preserveAspectRatio="none">
            <path d="M0,24 C15,22 22,18 35,19 C48,20 55,10 68,11 C80,12 88,6 100,4" fill="none" stroke={t.hero.spark} strokeWidth="2" />
          </svg>
        </Box>
        {[t.income, t.expense].map((color, i) => (
          <Box
            key={i}
            sx={{
              borderRadius: `${r}px`,
              backgroundColor: t.surface,
              border: `1px solid ${t.border}`,
              boxShadow: t.cardShadow,
              p: 0.9,
              display: "flex",
              flexDirection: "column",
              justifyContent: "space-between",
            }}
          >
            <Box sx={{ width: "55%", height: 4, borderRadius: 2, backgroundColor: t.textMuted, opacity: 0.6 }} />
            <Stack direction="row" spacing={0.4} alignItems="flex-end" sx={{ height: 22 }}>
              {[40, 70, 55, 90, 65].map((h, j) => (
                <Box key={j} sx={{ flex: 1, height: `${h}%`, borderRadius: "2px 2px 0 0", backgroundColor: color, opacity: j === 3 ? 1 : 0.55 }} />
              ))}
            </Stack>
          </Box>
        ))}
      </Box>
    </Box>
  );
}

function NavPreview({ nav }: { nav: NavLayout }) {
  const a = useTheme().app;
  const block = (sx: object) => <Box sx={{ backgroundColor: a.border, borderRadius: 1, ...sx }} />;

  return (
    <Box sx={{ backgroundColor: a.background, height: 110, p: 1.25, display: "flex", flexDirection: nav === "sidebar" ? "row" : "column", gap: 1 }}>
      {nav === "sidebar" ? (
        <Box sx={{ width: 42, backgroundColor: a.surface, border: `1px solid ${a.border}`, borderRadius: 1, p: 0.75 }}>
          {[0, 1, 2, 3].map((i) => (
            <Box key={i} sx={{ height: 5, borderRadius: 1, mb: 0.75, backgroundColor: i === 1 ? a.primary : a.border }} />
          ))}
        </Box>
      ) : (
        <Stack direction="row" spacing={0.75} alignItems="center" sx={{ height: 20, backgroundColor: a.surface, border: `1px solid ${a.border}`, borderRadius: 1, px: 0.75 }}>
          {[0, 1, 2, 3].map((i) => (
            <Box key={i} sx={{ width: 22, height: 5, borderRadius: 1, backgroundColor: i === 1 ? a.primary : a.border }} />
          ))}
        </Stack>
      )}
      <Box sx={{ flex: 1, display: "grid", gap: 0.75, gridTemplateColumns: "1fr 1fr 1fr" }}>
        {block({ gridColumn: "span 2" })}
        {block({})}
        {block({ gridColumn: "span 3" })}
      </Box>
    </Box>
  );
}

const MODE_OPTIONS: { value: ThemeModePreference; label: string; hint: string; icon: ReactNode }[] = [
  { value: "light", label: "Chiaro", hint: "Sempre chiaro", icon: <LightModeOutlined /> },
  { value: "dark", label: "Scuro", hint: "Sempre scuro", icon: <DarkModeOutlined /> },
  { value: "system", label: "Automatico", hint: "Segue il sistema operativo", icon: <SettingsBrightnessOutlined /> },
];

const NAV_OPTIONS: { value: NavLayout; label: string; hint: string }[] = [
  { value: "sidebar", label: "Barra laterale", hint: "Menu sempre visibile a sinistra, comprimibile" },
  { value: "topbar", label: "Barra superiore", hint: "Più spazio orizzontale per grafici e tabelle" },
];

export default function AppearanceSettings() {
  const a = useTheme().app;
  const { preferences, setPreset, setModePreference, setNavLayout } = useAppTheme();

  return (
    <Stack spacing={2.5}>
      <SurfaceCard title="Stile" subtitle="Tre direzioni visive diverse: colori, forme, tipografia e card. Le anteprime usano la modalità attuale.">
        <Box sx={{ display: "grid", gap: 2, gridTemplateColumns: { xs: "1fr", md: "repeat(3, 1fr)" } }}>
          {STYLE_PRESET_LIST.map((preset) => (
            <OptionCard
              key={preset.id}
              label={`Stile ${preset.name}`}
              selected={preferences.preset === preset.id}
              onClick={() => setPreset(preset.id)}
            >
              <PresetPreview preset={preset} />
              <Box sx={{ p: 1.75, borderTop: `1px solid ${a.border}` }}>
                <Typography sx={{ fontFamily: preset.headingFontFamily, fontWeight: 700, fontSize: "0.98rem" }}>{preset.name}</Typography>
                <Typography sx={{ fontSize: "0.78rem", color: a.textSecondary, mt: 0.25 }}>{preset.tagline}</Typography>
              </Box>
            </OptionCard>
          ))}
        </Box>
      </SurfaceCard>

      <SurfaceCard title="Tema" subtitle="Chiaro, scuro o automatico in base alle impostazioni del dispositivo.">
        <Box sx={{ display: "grid", gap: 1.5, gridTemplateColumns: { xs: "1fr", sm: "repeat(3, 1fr)" } }}>
          {MODE_OPTIONS.map((option) => (
            <OptionCard
              key={option.value}
              label={`Tema ${option.label}`}
              selected={preferences.mode === option.value}
              onClick={() => setModePreference(option.value)}
            >
              <Stack direction="row" spacing={1.5} alignItems="center" sx={{ p: 2 }}>
                <Box sx={{ color: preferences.mode === option.value ? a.primary : a.textSecondary, display: "flex" }}>{option.icon}</Box>
                <Box>
                  <Typography sx={{ fontWeight: 650, fontSize: "0.9rem" }}>{option.label}</Typography>
                  <Typography sx={{ fontSize: "0.76rem", color: a.textSecondary }}>{option.hint}</Typography>
                </Box>
              </Stack>
            </OptionCard>
          ))}
        </Box>
      </SurfaceCard>

      <SurfaceCard title="Navigazione" subtitle="Posizione del menu principale sugli schermi grandi. Su smartphone il menu è sempre a scomparsa.">
        <Box sx={{ display: "grid", gap: 2, gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" }, maxWidth: 640 }}>
          {NAV_OPTIONS.map((option) => (
            <OptionCard
              key={option.value}
              label={option.label}
              selected={preferences.nav === option.value}
              onClick={() => setNavLayout(option.value)}
            >
              <NavPreview nav={option.value} />
              <Box sx={{ p: 1.75, borderTop: `1px solid ${a.border}` }}>
                <Typography sx={{ fontWeight: 650, fontSize: "0.9rem" }}>{option.label}</Typography>
                <Typography sx={{ fontSize: "0.76rem", color: a.textSecondary }}>{option.hint}</Typography>
              </Box>
            </OptionCard>
          ))}
        </Box>
      </SurfaceCard>
    </Stack>
  );
}
