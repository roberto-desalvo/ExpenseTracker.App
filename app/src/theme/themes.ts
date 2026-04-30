import { createTheme } from "@mui/material/styles";

// Extend MUI palette with app-specific tokens
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
}

export const lightTheme = createTheme({
  palette: {
    mode: "light",
    background: {
      default: "#f8fafc",
      paper: "#ffffff",
    },
    text: {
      primary: "#1e293b",
      secondary: "#64748b",
    },
    divider: "rgba(30, 41, 59, 0.1)",
    custom: {
      appBackground: "#f8fafc",
      drawerBackground: "#ffffff",
      drawerBorder: "rgba(30, 41, 59, 0.1)",
      filterBorder: "rgba(30, 41, 59, 0.15)",
      filterBackground: "rgba(30, 41, 59, 0.04)",
      filterText: "#374151",
      accentColor: "#65a30d",
      accentHover: "rgba(101, 163, 13, 0.08)",
      amountPositive: "#16a34a",
      amountNegative: "#dc2626",
      badgeBackground: "rgba(30, 41, 59, 0.07)",
      badgeBorder: "rgba(30, 41, 59, 0.12)",
      badgeText: "#475569",
      tableHeaderText: "#94a3b8",
      rowBackground: "rgba(30, 41, 59, 0.02)",
      rowHover: "rgba(30, 41, 59, 0.05)",
      actionButtonColor: "#94a3b8",
      actionButtonBorder: "rgba(30, 41, 59, 0.15)",
      actionButtonBackground: "rgba(30, 41, 59, 0.03)",
      actionButtonHover: "rgba(30, 41, 59, 0.08)",
    },
  },
});

export const darkTheme = createTheme({
  palette: {
    mode: "dark",
    background: {
      default: "#0f172a",
      paper: "transparent",
    },
    text: {
      primary: "#f1f5f9",
      secondary: "#94a3b8",
    },
    divider: "rgba(255, 255, 255, 0.08)",
    custom: {
      appBackground: "#0f172a",
      drawerBackground: "#1e293b",
      drawerBorder: "rgba(255, 255, 255, 0.08)",
      filterBorder: "rgba(255, 255, 255, 0.12)",
      filterBackground: "rgba(255, 255, 255, 0.04)",
      filterText: "#e5e7eb",
      accentColor: "#a3e635",
      accentHover: "rgba(163, 230, 53, 0.08)",
      amountPositive: "#4ade80",
      amountNegative: "#f87171",
      badgeBackground: "rgba(255, 255, 255, 0.07)",
      badgeBorder: "rgba(255, 255, 255, 0.1)",
      badgeText: "#94a3b8",
      tableHeaderText: "#475569",
      rowBackground: "rgba(255, 255, 255, 0.03)",
      rowHover: "rgba(255, 255, 255, 0.06)",
      actionButtonColor: "#64748b",
      actionButtonBorder: "rgba(255, 255, 255, 0.1)",
      actionButtonBackground: "rgba(255, 255, 255, 0.04)",
      actionButtonHover: "rgba(255, 255, 255, 0.08)",
    },
  },
});
