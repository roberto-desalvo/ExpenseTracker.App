import React, { createContext, useContext, useEffect, useMemo, useState, ReactNode } from "react";
import { ThemeProvider as MuiThemeProvider } from "@mui/material/styles";
import CssBaseline from "@mui/material/CssBaseline";
import useMediaQuery from "@mui/material/useMediaQuery";
import { buildAppTheme } from "../theme/themes";
import { StylePresetId, STYLE_PRESETS } from "../theme/stylePresets";

export type ThemeMode = "light" | "dark";
export type ThemeModePreference = ThemeMode | "system";
export type NavLayout = "sidebar" | "topbar";

export interface AppearancePreferences {
  preset: StylePresetId;
  mode: ThemeModePreference;
  nav: NavLayout;
}

interface AppThemeContextType {
  /** modalità effettiva (risolve "system") */
  mode: ThemeMode;
  preferences: AppearancePreferences;
  toggleTheme: () => void;
  setPreset: (preset: StylePresetId) => void;
  setModePreference: (mode: ThemeModePreference) => void;
  setNavLayout: (nav: NavLayout) => void;
}

const STORAGE_KEY = "expense-tracker:appearance";

const DEFAULT_PREFERENCES: AppearancePreferences = {
  preset: "aurora",
  mode: "system",
  nav: "sidebar",
};

const readPreferences = (): AppearancePreferences => {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return DEFAULT_PREFERENCES;
    }

    const parsed = JSON.parse(raw) as Partial<AppearancePreferences>;
    return {
      preset: parsed.preset && parsed.preset in STYLE_PRESETS ? parsed.preset : DEFAULT_PREFERENCES.preset,
      mode: parsed.mode === "light" || parsed.mode === "dark" || parsed.mode === "system"
        ? parsed.mode
        : DEFAULT_PREFERENCES.mode,
      nav: parsed.nav === "topbar" ? "topbar" : "sidebar",
    };
  } catch {
    return DEFAULT_PREFERENCES;
  }
};

const AppThemeContext = createContext<AppThemeContextType | undefined>(undefined);

export const useAppTheme = () => {
  const ctx = useContext(AppThemeContext);
  if (!ctx) throw new Error("useAppTheme must be used inside AppThemeProvider");
  return ctx;
};

export const AppThemeProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [preferences, setPreferences] = useState<AppearancePreferences>(readPreferences);
  const prefersDark = useMediaQuery("(prefers-color-scheme: dark)", { noSsr: true });

  const mode: ThemeMode =
    preferences.mode === "system" ? (prefersDark ? "dark" : "light") : preferences.mode;

  useEffect(() => {
    try {
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify(preferences));
    } catch {
      // storage non disponibile: le preferenze valgono solo per la sessione
    }
  }, [preferences]);

  const theme = useMemo(() => buildAppTheme(preferences.preset, mode), [preferences.preset, mode]);

  const value = useMemo<AppThemeContextType>(
    () => ({
      mode,
      preferences,
      toggleTheme: () =>
        setPreferences((p) => ({ ...p, mode: mode === "light" ? "dark" : "light" })),
      setPreset: (preset) => setPreferences((p) => ({ ...p, preset })),
      setModePreference: (nextMode) => setPreferences((p) => ({ ...p, mode: nextMode })),
      setNavLayout: (nav) => setPreferences((p) => ({ ...p, nav })),
    }),
    [mode, preferences],
  );

  return (
    <AppThemeContext.Provider value={value}>
      <MuiThemeProvider theme={theme}>
        <CssBaseline />
        {children}
      </MuiThemeProvider>
    </AppThemeContext.Provider>
  );
};
