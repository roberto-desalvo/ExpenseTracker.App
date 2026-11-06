import { DOCUMENT } from "@angular/common";
import { DestroyRef, Injectable, computed, effect, inject, signal } from "@angular/core";
import { getSeriesColors } from "./chartColors";
import { ModeTokens, ResolvedMode, STYLE_PRESETS, StylePresetId } from "./stylePresets";

export type ThemeMode = ResolvedMode;
export type ThemeModePreference = ThemeMode | "system";
export type NavLayout = "sidebar" | "topbar";

export interface AppearancePreferences {
  preset: StylePresetId;
  mode: ThemeModePreference;
  nav: NavLayout;
}

/** Token effettivi dello stile attivo nella modalità risolta (usati dai grafici e dove serve un valore in TS). */
export interface AppTokens extends ModeTokens {
  presetId: StylePresetId;
  mode: ResolvedMode;
  radiusCard: number;
  radiusControl: number;
  fontFamily: string;
  headingFontFamily: string;
  chart: {
    series: readonly string[];
    other: string;
    grid: string;
    axis: string;
    net: string;
    /** quota investita delle uscite */
    invested: string;
  };
}

const STORAGE_KEY = "expense-tracker:appearance";

export const DEFAULT_APPEARANCE: AppearancePreferences = {
  preset: "aurora",
  mode: "system",
  nav: "sidebar",
};

/** Preferenze valide a partire da un valore letto da storage o dal BE (i campi non validi tornano ai predefiniti). */
export const normalizeAppearance = (raw: Partial<AppearancePreferences> | null | undefined): AppearancePreferences => ({
  preset: raw?.preset && raw.preset in STYLE_PRESETS ? raw.preset : DEFAULT_APPEARANCE.preset,
  mode: raw?.mode === "light" || raw?.mode === "dark" || raw?.mode === "system" ? raw.mode : DEFAULT_APPEARANCE.mode,
  nav: raw?.nav === "topbar" ? "topbar" : "sidebar",
});

const readPreferences = (): AppearancePreferences => {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    return raw ? normalizeAppearance(JSON.parse(raw) as Partial<AppearancePreferences>) : DEFAULT_APPEARANCE;
  } catch {
    return DEFAULT_APPEARANCE;
  }
};

export const buildAppTokens = (presetId: StylePresetId, mode: ResolvedMode): AppTokens => {
  const preset = STYLE_PRESETS[presetId];
  const t = preset[mode];
  const isDark = mode === "dark";
  return {
    ...t,
    presetId,
    mode,
    radiusCard: preset.radiusCard,
    radiusControl: preset.radiusControl,
    fontFamily: preset.fontFamily,
    headingFontFamily: preset.headingFontFamily,
    chart: {
      series: getSeriesColors(mode),
      other: isDark ? "#5f5e5a" : "#b8b6ad",
      grid: isDark ? "rgba(255,255,255,0.06)" : "rgba(15,23,42,0.07)",
      axis: t.textMuted,
      net: t.text,
      invested: isDark ? "#9085e9" : "#6250d6",
    },
  };
};

/** Variabili CSS derivate dai token: `--app-*` per gli stili dell'app, `--mat-sys-*` per i componenti Angular Material. */
const toCssVariables = (a: AppTokens): Record<string, string> => {
  const isDark = a.mode === "dark";
  const px = (value: number) => `${value}px`;
  return {
    "--app-background": a.background,
    "--app-surface": a.surface,
    "--app-surface-alt": a.surfaceAlt,
    "--app-border": a.border,
    "--app-border-strong": a.borderStrong,
    "--app-text": a.text,
    "--app-text-secondary": a.textSecondary,
    "--app-text-muted": a.textMuted,
    "--app-primary": a.primary,
    "--app-primary-hover": a.primaryHover,
    "--app-primary-contrast": a.primaryContrast,
    "--app-primary-soft": a.primarySoft,
    "--app-income": a.income,
    "--app-expense": a.expense,
    "--app-income-soft": a.incomeSoft,
    "--app-expense-soft": a.expenseSoft,
    "--app-invested": a.chart.invested,
    "--app-invested-soft": `color-mix(in srgb, ${a.chart.invested} 14%, transparent)`,
    "--app-card-shadow": a.cardShadow,
    "--app-sidebar-background": a.sidebarBackground,
    "--app-hero-background": a.hero.background,
    "--app-hero-text": a.hero.text,
    "--app-hero-subtle": a.hero.subtle,
    "--app-hero-spark": a.hero.spark,
    "--app-hero-border": a.hero.border,
    "--app-radius-card": px(a.radiusCard),
    "--app-radius-control": px(a.radiusControl),
    "--app-radius-option": px(Math.max(a.radiusControl - 2, 3)),
    "--app-radius-avatar": px(Math.max(a.radiusControl, 6)),
    "--app-font-family": a.fontFamily,
    "--app-heading-font-family": a.headingFontFamily,
    "--app-hover": isDark ? "rgba(255,255,255,0.04)" : "rgba(15,23,42,0.035)",
    "--app-option-hover": isDark ? "rgba(255,255,255,0.06)" : "rgba(15,23,42,0.05)",
    "--app-header-background": isDark ? "rgba(10,10,14,0.72)" : "rgba(255,255,255,0.78)",
    "--app-popup-shadow": isDark
      ? "0 16px 40px rgba(0,0,0,0.5), 0 2px 6px rgba(0,0,0,0.3)"
      : "0 12px 32px rgba(15,23,42,0.12), 0 2px 6px rgba(15,23,42,0.06)",
    "--app-dialog-shadow": isDark ? "0 24px 64px rgba(0,0,0,0.55)" : "0 24px 64px rgba(15,23,42,0.18)",
    "--app-backdrop": isDark ? "rgba(0,0,0,0.6)" : "rgba(15,23,42,0.32)",
    "--app-tooltip-background": isDark ? "#f4f4f5" : "#18181b",
    "--app-tooltip-text": isDark ? "#18181b" : "#fafafa",
    "--app-scrollbar": isDark ? "rgba(255,255,255,0.12)" : "rgba(15,23,42,0.14)",
    "--app-toggle-selected-shadow": isDark
      ? "0 0 0 1px rgba(255,255,255,0.08)"
      : "0 1px 2px rgba(15,23,42,0.08), 0 0 0 1px rgba(15,23,42,0.06)",

    // Angular Material (M3 system tokens)
    "--mat-sys-primary": a.primary,
    "--mat-sys-on-primary": a.primaryContrast,
    "--mat-sys-primary-container": a.primarySoft,
    "--mat-sys-on-primary-container": a.text,
    "--mat-sys-secondary-container": a.primarySoft,
    "--mat-sys-on-secondary-container": a.text,
    "--mat-sys-error": a.expense,
    "--mat-sys-background": a.background,
    "--mat-sys-on-background": a.text,
    "--mat-sys-surface": a.surface,
    "--mat-sys-on-surface": a.text,
    "--mat-sys-on-surface-variant": a.textSecondary,
    "--mat-sys-surface-container-lowest": a.surface,
    "--mat-sys-surface-container-low": a.surface,
    "--mat-sys-surface-container": a.surface,
    "--mat-sys-surface-container-high": a.surface,
    "--mat-sys-surface-container-highest": a.surfaceAlt,
    "--mat-sys-outline": a.borderStrong,
    "--mat-sys-outline-variant": a.border,
    "--mat-sys-inverse-surface": isDark ? "#f4f4f5" : "#18181b",
    "--mat-sys-inverse-on-surface": isDark ? "#18181b" : "#fafafa",
    "--mat-sys-corner-extra-small": px(Math.max(a.radiusControl - 4, 2)),
    "--mat-sys-corner-small": px(a.radiusControl),
    "--mat-sys-corner-medium": px(a.radiusControl + 2),
    "--mat-sys-corner-large": px(a.radiusCard),
    "--mat-sys-corner-extra-large": px(a.radiusCard),
    "--mat-sys-corner-full": "999px",
  };
};

/**
 * Aspetto dell'app (Impostazioni → Aspetto): stile, modalità chiara/scura/di sistema e layout di navigazione.
 * Le preferenze sono salvate a DB (`AppearanceStore`) e in localStorage come cache per il primo render;
 * i token dello stile attivo vengono applicati come variabili CSS su `:root`.
 */
@Injectable({ providedIn: "root" })
export class ThemeService {
  private readonly document = inject(DOCUMENT);

  readonly preferences = signal<AppearancePreferences>(readPreferences());
  private readonly prefersDark = signal(false);

  /** modalità effettiva (risolve "system") */
  readonly mode = computed<ThemeMode>(() => {
    const preference = this.preferences().mode;
    return preference === "system" ? (this.prefersDark() ? "dark" : "light") : preference;
  });

  readonly tokens = computed(() => buildAppTokens(this.preferences().preset, this.mode()));

  constructor() {
    const media = this.document.defaultView?.matchMedia?.("(prefers-color-scheme: dark)");
    if (media) {
      this.prefersDark.set(media.matches);
      const onChange = (event: MediaQueryListEvent) => this.prefersDark.set(event.matches);
      media.addEventListener("change", onChange);
      inject(DestroyRef).onDestroy(() => media.removeEventListener("change", onChange));
    }

    effect(() => {
      try {
        window.localStorage.setItem(STORAGE_KEY, JSON.stringify(this.preferences()));
      } catch {
        // storage non disponibile: le preferenze valgono solo per la sessione
      }
    });

    effect(() => this.applyTokens(this.tokens()));
  }

  toggleTheme() {
    const next: ThemeMode = this.mode() === "light" ? "dark" : "light";
    this.preferences.update((p) => ({ ...p, mode: next }));
  }

  setPreset(preset: StylePresetId) {
    this.preferences.update((p) => ({ ...p, preset }));
  }

  setModePreference(mode: ThemeModePreference) {
    this.preferences.update((p) => ({ ...p, mode }));
  }

  setNavLayout(nav: NavLayout) {
    this.preferences.update((p) => ({ ...p, nav }));
  }

  /** Sostituisce tutte le preferenze (es. quelle salvate a DB); `null` = predefinite. */
  replacePreferences(preferences: Partial<AppearancePreferences> | null) {
    this.preferences.set(normalizeAppearance(preferences));
  }

  private applyTokens(tokens: AppTokens) {
    const root = this.document.documentElement;
    for (const [name, value] of Object.entries(toCssVariables(tokens))) {
      root.style.setProperty(name, value);
    }
    root.style.colorScheme = tokens.mode;
    root.dataset["mode"] = tokens.mode;
    root.dataset["preset"] = tokens.presetId;
  }
}
