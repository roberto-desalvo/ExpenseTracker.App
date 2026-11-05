import { ParamMap, Params } from "@angular/router";
import { DrillDownFilter, MovementType } from "../state/transactions-table.store";
import { periodRange } from "../utils/date.utilities";

/** Query param di Movimenti per aprirla già filtrata (drill-down da un grafico). */
export const DRILL_DOWN_PARAMS = {
  period: "periodo",
  category: "categoria",
  type: "tipo",
  /** pagina di provenienza, per il pulsante di ritorno */
  source: "da",
} as const;

const TYPE_TO_PARAM: Record<MovementType, string | null> = { all: null, incomes: "entrate", outcomes: "uscite" };

export const drillDownParams = (filter: DrillDownFilter, source: string): Params => ({
  [DRILL_DOWN_PARAMS.period]: filter.period,
  [DRILL_DOWN_PARAMS.category]: filter.categoryIds.length ? filter.categoryIds.join(",") : null,
  [DRILL_DOWN_PARAMS.type]: TYPE_TO_PARAM[filter.movementType],
  [DRILL_DOWN_PARAMS.source]: source,
});

/** Filtri del drill-down dai query param; null se manca un periodo valido. */
export const parseDrillDown = (query: ParamMap): DrillDownFilter | null => {
  const period = query.get(DRILL_DOWN_PARAMS.period);
  if (!period || !periodRange(period)) return null;
  const type = query.get(DRILL_DOWN_PARAMS.type);
  const categoryIds = (query.get(DRILL_DOWN_PARAMS.category) ?? "")
    .split(",")
    .filter(Boolean)
    .map(Number)
    .filter(Number.isInteger);
  return {
    period,
    categoryIds,
    movementType: type === "entrate" ? "incomes" : type === "uscite" ? "outcomes" : "all",
  };
};
