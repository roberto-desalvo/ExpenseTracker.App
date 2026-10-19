/**
 * Assegna i colori categoriali alle entità (conti, categorie) in base a un ordine
 * stabile (id crescente), così un filtro che nasconde una serie non ricolora le altre.
 * Oltre gli slot disponibili si usa il colore neutro "altro".
 */
export const buildEntityColorMap = (
  ids: number[],
  series: readonly string[],
  other: string,
): Map<number, string> => {
  const sorted = [...new Set(ids)].sort((x, y) => x - y);
  return new Map(sorted.map((id, index) => [id, index < series.length ? series[index] : other]));
};
