import { AnalyticsCardLayout, AnalyticsLayout, CardWidth } from "../models/AnalyticsLayout";

export type AnalyticsCardId =
  "summary" | "cash-flow" | "net-worth" | "period-rank" | "category-periods" | "category-rank" | "planning";

/** Verticali (liste, classifiche): 1/3 o 1/2. Orizzontali (grafici per periodo): 1/2, 2/3 o intera. */
export type CardOrientation = "vertical" | "horizontal";

export interface AnalyticsCardDefinition {
  id: AnalyticsCardId;
  title: string;
  description: string;
  icon: string;
  orientation: CardOrientation;
  defaultWidth: CardWidth;
}

export interface PlacedAnalyticsCard {
  id: AnalyticsCardId;
  width: CardWidth;
}

export const ALLOWED_WIDTHS: Record<CardOrientation, readonly CardWidth[]> = {
  vertical: ["third", "half"],
  horizontal: ["half", "twoThirds", "full"],
};

/** Colonne occupate sulla griglia a 6 colonne. */
export const WIDTH_SPAN: Record<CardWidth, number> = { third: 2, half: 3, twoThirds: 4, full: 6 };

export const WIDTH_LABEL: Record<CardWidth, string> = {
  third: "1/3",
  half: "1/2",
  twoThirds: "2/3",
  full: "Intera",
};

export const ANALYTICS_CARDS: readonly AnalyticsCardDefinition[] = [
  {
    id: "summary",
    title: "Sintesi del periodo",
    description: "Patrimonio a inizio e fine periodo, entrate, uscite, tassi di risparmio e investimento",
    icon: "summarize",
    orientation: "horizontal",
    defaultWidth: "full",
  },
  {
    id: "cash-flow",
    title: "Flusso di cassa",
    description: "Entrate, uscite e investimenti per periodo, con il saldo netto",
    icon: "bar_chart",
    orientation: "horizontal",
    defaultWidth: "full",
  },
  {
    id: "net-worth",
    title: "Patrimonio",
    description: "Saldo medio di tutti i conti per periodo",
    icon: "show_chart",
    orientation: "horizontal",
    defaultWidth: "half",
  },
  {
    id: "period-rank",
    title: "Classifica periodi",
    description: "I mesi o gli anni migliori e peggiori",
    icon: "leaderboard",
    orientation: "vertical",
    defaultWidth: "half",
  },
  {
    id: "category-periods",
    title: "Categorie per periodo",
    description: "Composizione per categoria di uscite o entrate in ogni periodo",
    icon: "stacked_bar_chart",
    orientation: "horizontal",
    defaultWidth: "twoThirds",
  },
  {
    id: "category-rank",
    title: "Classifica categorie",
    description: "Totale per categoria nel periodo selezionato",
    icon: "format_list_numbered",
    orientation: "vertical",
    defaultWidth: "third",
  },
  {
    id: "planning",
    title: "Pianificazioni",
    description: "Spesa prevista ed effettiva per periodo e scostamento dalla previsione",
    icon: "event_note",
    orientation: "horizontal",
    defaultWidth: "full",
  },
];

export const CARD_BY_ID = new Map(ANALYTICS_CARDS.map((c) => [c.id, c]));

/** Riproduce la disposizione storica della pagina. */
export const DEFAULT_ANALYTICS_LAYOUT: readonly PlacedAnalyticsCard[] = ANALYTICS_CARDS.map((c) => ({
  id: c.id,
  width: c.defaultWidth,
}));

export const allowedWidthsOf = (id: AnalyticsCardId) => ALLOWED_WIDTHS[CARD_BY_ID.get(id)!.orientation];

/**
 * Layout salvato → layout utilizzabile: `null` = predefinito; scarta card sconosciute (es. rimosse dal catalogo)
 * e duplicate; una larghezza non ammessa per l'orientamento della card torna alla sua larghezza predefinita.
 */
export const normalizeLayout = (raw: AnalyticsLayout | null | undefined): PlacedAnalyticsCard[] => {
  if (!raw || !Array.isArray(raw.cards)) return DEFAULT_ANALYTICS_LAYOUT.map((c) => ({ ...c }));
  const seen = new Set<string>();
  return raw.cards.flatMap((card: AnalyticsCardLayout) => {
    const definition = CARD_BY_ID.get(card?.id as AnalyticsCardId);
    if (!definition || seen.has(definition.id)) return [];
    seen.add(definition.id);
    const width = ALLOWED_WIDTHS[definition.orientation].includes(card.width) ? card.width : definition.defaultWidth;
    return [{ id: definition.id, width }];
  });
};

export const sameLayout = (a: readonly PlacedAnalyticsCard[], b: readonly PlacedAnalyticsCard[]) =>
  a.length === b.length && a.every((card, i) => card.id === b[i].id && card.width === b[i].width);
