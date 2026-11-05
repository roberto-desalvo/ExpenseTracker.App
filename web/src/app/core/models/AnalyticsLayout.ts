/** Larghezza di una card sulla griglia della pagina Analisi. */
export type CardWidth = "third" | "half" | "twoThirds" | "full";

export interface AnalyticsCardLayout {
  id: string;
  width: CardWidth;
}

/** Layout salvato a DB: ordine delle card = ordine della lista; le card assenti sono nascoste. */
export interface AnalyticsLayout {
  cards: AnalyticsCardLayout[];
}
