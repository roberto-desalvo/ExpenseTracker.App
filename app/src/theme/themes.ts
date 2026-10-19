import { alpha, createTheme, Theme } from "@mui/material/styles";
import type {} from "@mui/x-date-pickers/themeAugmentation";
import { getSeriesColors } from "./chartColors";
import {
  ModeTokens,
  ResolvedMode,
  STYLE_PRESETS,
  StylePreset,
  StylePresetId,
} from "./stylePresets";

export interface AppTokens extends ModeTokens {
  presetId: StylePresetId;
  mode: ResolvedMode;
  radiusCard: number;
  radiusControl: number;
  headingFontFamily: string;
  chart: {
    series: readonly string[];
    other: string;
    grid: string;
    axis: string;
    net: string;
  };
}

// Estende il tema MUI con i token dell'app
declare module "@mui/material/styles" {
  interface Palette {
    custom: {
      appBackground: string;
      drawerBackground: string;
      drawerBorder: string;
      filterBorder: string;
      filterBackground: string;
      filterText: string;
      accentColor: string;
      accentHover: string;
      amountPositive: string;
      amountNegative: string;
      badgeBackground: string;
      badgeBorder: string;
      badgeText: string;
      tableHeaderText: string;
      rowBackground: string;
      rowHover: string;
      actionButtonColor: string;
      actionButtonBorder: string;
      actionButtonBackground: string;
      actionButtonHover: string;
    };
  }
  interface PaletteOptions {
    custom?: Partial<Palette["custom"]>;
  }
  interface Theme {
    app: AppTokens;
  }
  interface ThemeOptions {
    app?: AppTokens;
  }
}

export const buildAppTheme = (presetId: StylePresetId, mode: ResolvedMode): Theme => {
  const preset: StylePreset = STYLE_PRESETS[presetId];
  const t: ModeTokens = preset[mode];
  const isDark = mode === "dark";

  const app: AppTokens = {
    ...t,
    presetId,
    mode,
    radiusCard: preset.radiusCard,
    radiusControl: preset.radiusControl,
    headingFontFamily: preset.headingFontFamily,
    chart: {
      series: getSeriesColors(mode),
      other: isDark ? "#5f5e5a" : "#b8b6ad",
      grid: isDark ? "rgba(255,255,255,0.06)" : "rgba(15,23,42,0.07)",
      axis: t.textMuted,
      net: t.text,
    },
  };

  const hover = isDark ? "rgba(255,255,255,0.04)" : "rgba(15,23,42,0.035)";

  return createTheme({
    app,
    shape: { borderRadius: preset.radiusControl },
    palette: {
      mode,
      primary: { main: t.primary, dark: t.primaryHover, contrastText: t.primaryContrast },
      success: { main: t.income },
      error: { main: t.expense },
      background: { default: t.background, paper: t.surface },
      text: { primary: t.text, secondary: t.textSecondary, disabled: t.textMuted },
      divider: t.border,
      action: { hover },
      custom: {
        appBackground: t.background,
        drawerBackground: t.surface,
        drawerBorder: t.border,
        filterBorder: t.border,
        filterBackground: t.surfaceAlt,
        filterText: t.textSecondary,
        accentColor: t.primary,
        accentHover: t.primarySoft,
        amountPositive: t.income,
        amountNegative: t.expense,
        badgeBackground: t.surfaceAlt,
        badgeBorder: t.border,
        badgeText: t.textSecondary,
        tableHeaderText: t.textMuted,
        rowBackground: "transparent",
        rowHover: hover,
        actionButtonColor: t.textMuted,
        actionButtonBorder: "transparent",
        actionButtonBackground: "transparent",
        actionButtonHover: hover,
      },
    },
    typography: {
      fontFamily: preset.fontFamily,
      h4: { fontFamily: preset.headingFontFamily, fontWeight: 700, letterSpacing: "-0.025em" },
      h5: { fontFamily: preset.headingFontFamily, fontWeight: 700, letterSpacing: "-0.02em" },
      h6: { fontFamily: preset.headingFontFamily, fontWeight: 650, letterSpacing: "-0.01em" },
      subtitle1: { fontWeight: 600 },
      button: { textTransform: "none", fontWeight: 600, letterSpacing: 0 },
      overline: { fontWeight: 600, letterSpacing: "0.08em", lineHeight: 1.6 },
    },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: { backgroundColor: t.background },
          "*::-webkit-scrollbar": { width: 10, height: 10 },
          "*::-webkit-scrollbar-thumb": {
            backgroundColor: isDark ? "rgba(255,255,255,0.12)" : "rgba(15,23,42,0.14)",
            borderRadius: 10,
            border: `2px solid ${t.background}`,
          },
        },
      },
      MuiPaper: {
        styleOverrides: { root: { backgroundImage: "none" } },
      },
      MuiButton: {
        defaultProps: { disableElevation: true },
        styleOverrides: {
          root: { borderRadius: preset.radiusControl, paddingInline: 16, minHeight: 38 },
          outlined: {
            borderColor: t.borderStrong,
            color: t.text,
            "&:hover": { borderColor: t.borderStrong, backgroundColor: hover },
          },
          text: { color: t.textSecondary, "&:hover": { backgroundColor: hover } },
        },
      },
      MuiIconButton: {
        styleOverrides: { root: { borderRadius: preset.radiusControl } },
      },
      MuiOutlinedInput: {
        styleOverrides: {
          root: {
            borderRadius: preset.radiusControl,
            backgroundColor: t.surface,
            "& .MuiOutlinedInput-notchedOutline": { borderColor: t.borderStrong },
            "&:hover .MuiOutlinedInput-notchedOutline": { borderColor: alpha(t.text, 0.3) },
            "&.Mui-focused .MuiOutlinedInput-notchedOutline": {
              borderColor: t.primary,
              borderWidth: 1.5,
            },
          },
        },
      },
      MuiMenu: {
        defaultProps: { disableScrollLock: true },
        styleOverrides: {
          paper: {
            marginTop: 6,
            borderRadius: preset.radiusControl + 2,
            border: `1px solid ${t.border}`,
            boxShadow: isDark ? "0 16px 40px rgba(0,0,0,0.5)" : "0 12px 32px rgba(15,23,42,0.12)",
          },
        },
      },
      MuiPopover: {
        styleOverrides: {
          paper: { border: `1px solid ${t.border}` },
        },
      },
      MuiMenuItem: {
        styleOverrides: {
          root: {
            fontSize: "0.88rem",
            minHeight: 38,
            borderRadius: Math.max(preset.radiusControl - 2, 2),
            marginInline: 6,
            "&.Mui-selected": { backgroundColor: t.primarySoft },
            "&.Mui-selected:hover": { backgroundColor: t.primarySoft },
          },
        },
      },
      MuiDialog: {
        styleOverrides: {
          paper: {
            borderRadius: preset.radiusCard,
            border: `1px solid ${t.border}`,
            boxShadow: isDark ? "0 24px 64px rgba(0,0,0,0.55)" : "0 24px 64px rgba(15,23,42,0.18)",
          },
        },
      },
      MuiBackdrop: {
        styleOverrides: {
          root: {
            "&:not(.MuiBackdrop-invisible)": {
              backgroundColor: isDark ? "rgba(0,0,0,0.6)" : "rgba(15,23,42,0.32)",
              backdropFilter: "blur(3px)",
            },
          },
        },
      },
      MuiTooltip: {
        styleOverrides: {
          tooltip: {
            backgroundColor: isDark ? "#f4f4f5" : "#18181b",
            color: isDark ? "#18181b" : "#fafafa",
            fontSize: "0.75rem",
            borderRadius: 6,
            padding: "6px 10px",
          },
        },
      },
      MuiChip: {
        styleOverrides: {
          root: { borderRadius: Math.max(preset.radiusControl - 2, 4), fontWeight: 500 },
        },
      },
      MuiTableCell: {
        styleOverrides: {
          root: { borderColor: t.border },
          head: {
            color: t.textMuted,
            fontSize: "0.7rem",
            fontWeight: 600,
            textTransform: "uppercase",
            letterSpacing: "0.08em",
          },
        },
      },
      MuiToggleButton: {
        styleOverrides: {
          root: {
            textTransform: "none",
            fontWeight: 600,
            fontSize: "0.82rem",
            border: "none",
            borderRadius: `${Math.max(preset.radiusControl - 2, 3)}px !important`,
            color: t.textSecondary,
            padding: "5px 12px",
            "&.Mui-selected": {
              backgroundColor: t.surface,
              color: t.text,
              boxShadow: isDark
                ? "0 0 0 1px rgba(255,255,255,0.08)"
                : "0 1px 2px rgba(15,23,42,0.08), 0 0 0 1px rgba(15,23,42,0.06)",
            },
            "&.Mui-selected:hover": { backgroundColor: t.surface },
          },
        },
      },
      MuiToggleButtonGroup: {
        styleOverrides: {
          root: {
            backgroundColor: t.surfaceAlt,
            borderRadius: preset.radiusControl,
            padding: 3,
            gap: 2,
          },
        },
      },
      MuiTab: {
        styleOverrides: {
          root: { textTransform: "none", fontWeight: 600, minHeight: 44 },
        },
      },
      MuiSwitch: {
        styleOverrides: {
          switchBase: { "&.Mui-checked + .MuiSwitch-track": { opacity: 1 } },
        },
      },
      MuiTablePagination: {
        styleOverrides: {
          root: { color: t.textSecondary, borderBottom: "none" },
        },
      },
    },
  });
};
