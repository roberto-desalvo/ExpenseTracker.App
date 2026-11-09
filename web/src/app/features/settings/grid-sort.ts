type Rect = Pick<DOMRect, "top" | "bottom" | "left" | "right">;

/**
 * Isteresi per il riordino a griglia (CDK, orientamento `mixed`): la card sotto il puntatore cede il posto
 * solo quando il puntatore ne supera la metà nella direzione dello spostamento (orizzontale se è sulla stessa riga
 * del segnaposto, verticale altrimenti). Senza, con card di altezza/larghezza diverse il puntatore resta sopra la card
 * appena scambiata e il CDK la scambia di nuovo all'evento successivo: il segnaposto oscilla e torna al punto di partenza.
 */
export function shouldSwap(pointer: { x: number; y: number }, target: Rect, placeholder: Rect, forward: boolean): boolean {
  const sameRow = target.top < placeholder.bottom && placeholder.top < target.bottom;
  const [position, start, end] = sameRow
    ? [pointer.x, target.left, target.right]
    : [pointer.y, target.top, target.bottom];
  const middle = (start + end) / 2;
  return forward ? position >= middle : position <= middle;
}
