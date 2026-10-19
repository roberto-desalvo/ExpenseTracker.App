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

  // Stile unico per tutti i pannelli a discesa (Menu, Select, Autocomplete, calendario)
  const popupPaper = {
    marginTop: 6,
    backgroundColor: t.surface,
    backgroundImage: "none",
    borderRadius: preset.radiusControl + 2,
    border: `1px solid ${t.border}`,
    boxShadow: isDark
      ? "0 16px 40px rgba(0,0,0,0.5), 0 2px 6px rgba(0,0,0,0.3)"
      : "0 12px 32px rgba(15,23,42,0.12), 0 2px 6px rgba(15,23,42,0.06)",
  };
  const optionRadius = Math.max(preset.radiusControl - 2, 3);
  const optionBase = {
    fontSize: "0.875rem",
    minHeight: 36,
    borderRadius: optionRadius,
    marginInline: 4,
    paddingInline: 10,
    gap: 4,
    color: t.text,
  };
  // spunta a destra della voce selezionata (solo liste senza checkbox)
  const selectedCheck = {
    content: "\"\"",
    marginLeft: "auto",
    paddingLeft: 12,
    width: 16,
    height: 16,
    flexShrink: 0,
    backgroundColor: t.primary,
    WebkitMask: "url(\"data:image/svg+xml,%3Csvg xmlns=%27http://www.w3.org/2000/svg%27 viewBox=%270 0 24 24%27%3E%3Cpath d=%27M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4z%27/%3E%3C/svg%3E\") no-repeat right center / 16px",
    mask: "url(\"data:image/svg+xml,%3Csvg xmlns=%27http://www.w3.org/2000/svg%27 viewBox=%270 0 24 24%27%3E%3Cpath d=%27M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4z%27/%3E%3C/svg%3E\") no-repeat right center / 16px",
  };
  const optionHover = isDark ? "rgba(255,255,255,0.06)" : "rgba(15,23,42,0.05)";

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
          paper: popupPaper,
          list: { padding: 4, display: "flex", flexDirection: "column", gap: 1 },
        },
      },
      MuiPopover: {
        styleOverrides: {
          paper: { border: `1px solid ${t.border}`, backgroundImage: "none" },
        },
      },
      MuiSelect: {
        defaultProps: {
          // il menu si apre sotto il campo invece di coprirlo
          MenuProps: {
            anchorOrigin: { vertical: "bottom", horizontal: "left" },
            transformOrigin: { vertical: "top", horizontal: "left" },
            slotProps: { paper: { sx: { maxHeight: 360 } } },
          },
        },
        styleOverrides: {
          icon: { color: t.textMuted },
        },
      },
      MuiMenuItem: {
        styleOverrides: {
          root: {
            ...optionBase,
            "&:hover": { backgroundColor: optionHover },
            "&.Mui-focusVisible": { backgroundColor: optionHover },
            "&.Mui-selected": { backgroundColor: t.primarySoft, fontWeight: 600 },
            "&.Mui-selected:hover, &.Mui-selected.Mui-focusVisible": { backgroundColor: t.primarySoft },
            "& .MuiListItemIcon-root": { minWidth: 30, color: t.textSecondary },
            "& .MuiListItemText-root": { margin: 0 },
            "& .MuiListItemText-primary": { fontSize: "0.875rem", fontWeight: "inherit" },
            "& .MuiListItemText-secondary": { fontSize: "0.75rem", color: t.textMuted },
            "& .MuiCheckbox-root": { padding: 4, marginLeft: -4, marginRight: 4 },
          },
        },
      },
      MuiCheckbox: {
        defaultProps: { size: "small", disableRipple: true },
        styleOverrides: {
          root: {
            color: t.borderStrong,
            "&.Mui-checked, &.MuiCheckbox-indeterminate": { color: t.primary },
          },
        },
      },
      MuiAutocomplete: {
        styleOverrides: {
          paper: { ...popupPaper, overflow: "hidden" },
          listbox: {
            padding: 4,
            maxHeight: 320,
            "& .MuiAutocomplete-option": {
              ...optionBase,
              display: "flex",
              alignItems: "center",
              '&[aria-selected="true"]': { backgroundColor: t.primarySoft, fontWeight: 600 },
              '&[aria-selected="true"]::after': selectedCheck,
              "&.Mui-focused": { backgroundColor: optionHover },
              '&[aria-selected="true"].Mui-focused': { backgroundColor: t.primarySoft },
            },
          },
          noOptions: { fontSize: "0.85rem", color: t.textMuted },
          loading: { fontSize: "0.85rem", color: t.textMuted },
          groupLabel: {
            fontSize: "0.7rem",
            fontWeight: 600,
            textTransform: "uppercase",
            letterSpacing: "0.08em",
            color: t.textMuted,
            backgroundColor: t.surface,
          },
          tag: { height: 26, fontSize: "0.78rem" },
          popupIndicator: { color: t.textMuted },
          clearIndicator: { color: t.textMuted },
        },
      },
      MuiPickersPopper: {
        styleOverrides: {
          paper: popupPaper,
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
