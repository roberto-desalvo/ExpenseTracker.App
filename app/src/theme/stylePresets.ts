/**
 * Stili visivi selezionabili dall'utente (Impostazioni → Aspetto).
 * Ogni stile definisce i token per la modalità chiara e scura; il tema MUI
 * viene costruito a partire da questi token in `themes.ts`.
 */

export type StylePresetId = "aurora" | "ledger" | "graphite";
export type ResolvedMode = "light" | "dark";

export interface HeroTokens {
  background: string;
  text: string;
  subtle: string;
  spark: string;
  border: string;
}

export interface ModeTokens {
  background: string;
  surface: string;
  surfaceAlt: string;
  border: string;
  borderStrong: string;
  text: string;
  textSecondary: string;
  textMuted: string;
  primary: string;
  primaryHover: string;
  primaryContrast: string;
  primarySoft: string;
  income: string;
  expense: string;
  incomeSoft: string;
  expenseSoft: string;
  cardShadow: string;
  sidebarBackground: string;
  hero: HeroTokens;
}

export interface StylePreset {
  id: StylePresetId;
  name: string;
  tagline: string;
  fontFamily: string;
  headingFontFamily: string;
  /** raggio delle card in px */
  radiusCard: number;
  /** raggio di bottoni, input e controlli in px */
  radiusControl: number;
  light: ModeTokens;
  dark: ModeTokens;
}

const systemSans = 'system-ui, -apple-system, "Segoe UI", Roboto, sans-serif';

export const STYLE_PRESETS: Record<StylePresetId, StylePreset> = {
  aurora: {
    id: "aurora",
    name: "Aurora",
    tagline: "Morbido e luminoso, accenti indaco e card sospese",
    fontFamily: `"Inter", ${systemSans}`,
    headingFontFamily: `"Inter", ${systemSans}`,
    radiusCard: 18,
    radiusControl: 10,
    light: {
      background: "#f4f5fa",
      surface: "#ffffff",
      surfaceAlt: "#f0f1f8",
      border: "rgba(15, 23, 42, 0.07)",
      borderStrong: "rgba(15, 23, 42, 0.14)",
      text: "#0f172a",
      textSecondary: "#545d73",
      textMuted: "#8a91a5",
      primary: "#5046e5",
      primaryHover: "#4338ca",
      primaryContrast: "#ffffff",
      primarySoft: "rgba(80, 70, 229, 0.09)",
      income: "#0e9f6e",
      expense: "#e5484d",
      incomeSoft: "rgba(14, 159, 110, 0.10)",
      expenseSoft: "rgba(229, 72, 77, 0.09)",
      cardShadow: "0 1px 2px rgba(15,23,42,0.04), 0 8px 24px rgba(15,23,42,0.06)",
      sidebarBackground: "#ffffff",
      hero: {
        background: "linear-gradient(135deg, #4f46e5 0%, #7c3aed 60%, #a855f7 100%)",
        text: "#ffffff",
        subtle: "rgba(255,255,255,0.74)",
        spark: "#ffffff",
        border: "transparent",
      },
    },
    dark: {
      background: "#0b0d17",
      surface: "#131627",
      surfaceAlt: "#1a1e33",
      border: "rgba(255, 255, 255, 0.07)",
      borderStrong: "rgba(255, 255, 255, 0.14)",
      text: "#eef0f8",
      textSecondary: "#a5abc2",
      textMuted: "#6f7591",
      primary: "#8b8cf8",
      primaryHover: "#a5a6fb",
      primaryContrast: "#0b0d17",
      primarySoft: "rgba(139, 140, 248, 0.14)",
      income: "#3dd68c",
      expense: "#ff6b70",
      incomeSoft: "rgba(61, 214, 140, 0.12)",
      expenseSoft: "rgba(255, 107, 112, 0.12)",
      cardShadow: "0 1px 0 rgba(255,255,255,0.03) inset, 0 12px 32px rgba(0,0,0,0.35)",
      sidebarBackground: "#0f1220",
      hero: {
        background: "linear-gradient(135deg, #3730a3 0%, #5b21b6 60%, #7e22ce 100%)",
        text: "#ffffff",
        subtle: "rgba(255,255,255,0.7)",
        spark: "#ffffff",
        border: "rgba(255,255,255,0.08)",
      },
    },
  },
  ledger: {
    id: "ledger",
    name: "Ledger",
    tagline: "Classico da private banking: toni caldi, verde petrolio, bordi netti",
    fontFamily: `"Manrope", ${systemSans}`,
    headingFontFamily: `"Manrope", ${systemSans}`,
    radiusCard: 12,
    radiusControl: 8,
    light: {
      background: "#f5f3ee",
      surface: "#fffefb",
      surfaceAlt: "#f0ede5",
      border: "rgba(28, 25, 23, 0.10)",
      borderStrong: "rgba(28, 25, 23, 0.18)",
      text: "#1c1917",
      textSecondary: "#57534e",
      textMuted: "#8b857c",
      primary: "#0f766e",
      primaryHover: "#115e59",
      primaryContrast: "#ffffff",
      primarySoft: "rgba(15, 118, 110, 0.09)",
      income: "#15803d",
      expense: "#c2410c",
      incomeSoft: "rgba(21, 128, 61, 0.09)",
      expenseSoft: "rgba(194, 65, 12, 0.08)",
      cardShadow: "none",
      sidebarBackground: "#fbfaf6",
      hero: {
        background: "#0f3d36",
        text: "#f5f3ea",
        subtle: "rgba(245,243,234,0.7)",
        spark: "#5eead4",
        border: "transparent",
      },
    },
    dark: {
      background: "#0e1110",
      surface: "#161a19",
      surfaceAlt: "#1d2321",
      border: "rgba(255, 255, 255, 0.08)",
      borderStrong: "rgba(255, 255, 255, 0.16)",
      text: "#ecebe6",
      textSecondary: "#b4b0a6",
      textMuted: "#7d7a72",
      primary: "#2dd4bf",
      primaryHover: "#5eead4",
      primaryContrast: "#04201d",
      primarySoft: "rgba(45, 212, 191, 0.12)",
      income: "#4ade80",
      expense: "#fb923c",
      incomeSoft: "rgba(74, 222, 128, 0.11)",
      expenseSoft: "rgba(251, 146, 60, 0.11)",
      cardShadow: "none",
      sidebarBackground: "#121615",
      hero: {
        background: "#11302b",
        text: "#f5f3ea",
        subtle: "rgba(245,243,234,0.68)",
        spark: "#5eead4",
        border: "rgba(94,234,212,0.16)",
      },
    },
  },
  graphite: {
    id: "graphite",
    name: "Graphite",
    tagline: "Minimal monocromatico, angoli vivi e un solo accento lime",
    fontFamily: `"IBM Plex Sans", ${systemSans}`,
    headingFontFamily: `"IBM Plex Sans", ${systemSans}`,
    radiusCard: 6,
    radiusControl: 4,
    light: {
      background: "#fafafa",
      surface: "#ffffff",
      surfaceAlt: "#f4f4f5",
      border: "rgba(9, 9, 11, 0.10)",
      borderStrong: "rgba(9, 9, 11, 0.20)",
      text: "#09090b",
      textSecondary: "#52525b",
      textMuted: "#a1a1aa",
      primary: "#18181b",
      primaryHover: "#3f3f46",
      primaryContrast: "#ffffff",
      primarySoft: "rgba(24, 24, 27, 0.06)",
      income: "#16a34a",
      expense: "#dc2626",
      incomeSoft: "rgba(22, 163, 74, 0.08)",
      expenseSoft: "rgba(220, 38, 38, 0.07)",
      cardShadow: "none",
      sidebarBackground: "#fafafa",
      hero: {
        background: "#09090b",
        text: "#fafafa",
        subtle: "rgba(250,250,250,0.62)",
        spark: "#a3e635",
        border: "transparent",
      },
    },
    dark: {
      background: "#09090b",
      surface: "#111113",
      surfaceAlt: "#18181b",
      border: "rgba(255, 255, 255, 0.09)",
      borderStrong: "rgba(255, 255, 255, 0.18)",
      text: "#fafafa",
      textSecondary: "#a1a1aa",
      textMuted: "#71717a",
      primary: "#fafafa",
      primaryHover: "#e4e4e7",
      primaryContrast: "#09090b",
      primarySoft: "rgba(250, 250, 250, 0.08)",
      income: "#4ade80",
      expense: "#f87171",
      incomeSoft: "rgba(74, 222, 128, 0.10)",
      expenseSoft: "rgba(248, 113, 113, 0.10)",
      cardShadow: "none",
      sidebarBackground: "#09090b",
      hero: {
        background: "#18181b",
        text: "#fafafa",
        subtle: "rgba(250,250,250,0.6)",
        spark: "#a3e635",
        border: "rgba(255,255,255,0.09)",
      },
    },
  },
};

export const STYLE_PRESET_LIST: StylePreset[] = Object.values(STYLE_PRESETS);
