/**
 * Palette categoriale dei grafici (identità delle serie: conti, categorie).
 * L'ordine è parte della sicurezza per daltonici: le coppie adiacenti sono state
 * validate (CVD ΔE ≥ 8) sulle superfici chiare e scure di tutti gli stili.
 * Gli slot si assegnano sempre in ordine fisso, mai ciclando: oltre l'ottava
 * serie si usa il colore "altro" neutro.
 */
export const CHART_SERIES_COLORS_LIGHT = [
  "#2a78d6",
  "#eb6834",
  "#1baf7a",
  "#eda100",
  "#e87ba4",
  "#008300",
  "#6250d6",
  "#e34948",
] as const;

export const CHART_SERIES_COLORS_DARK = [
  "#3987e5",
  "#d95926",
  "#199e70",
  "#c98500",
  "#d55181",
  "#008300",
  "#9085e9",
  "#e66767",
] as const;

/** Mantenuto per i componenti che non distinguono la modalità. */
export const CHART_SERIES_COLORS = CHART_SERIES_COLORS_LIGHT;

export const getSeriesColors = (mode: "light" | "dark"): readonly string[] =>
  mode === "dark" ? CHART_SERIES_COLORS_DARK : CHART_SERIES_COLORS_LIGHT;
