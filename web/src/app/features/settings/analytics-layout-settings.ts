import { CdkDrag, CdkDragDrop, CdkDropList, DragDropModule, moveItemInArray } from "@angular/cdk/drag-drop";
import { DOCUMENT } from "@angular/common";
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, linkedSignal } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatButtonToggleModule } from "@angular/material/button-toggle";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltipModule } from "@angular/material/tooltip";
import { NotificationService } from "../../core/api/notification.service";
import {
  ANALYTICS_CARDS,
  AnalyticsCardDefinition,
  AnalyticsCardId,
  CARD_BY_ID,
  DEFAULT_ANALYTICS_LAYOUT,
  PlacedAnalyticsCard,
  WIDTH_LABEL,
  WIDTH_SPAN,
  allowedWidthsOf,
  sameLayout,
} from "../../core/config/analytics-cards";
import { CardWidth } from "../../core/models/AnalyticsLayout";
import { AnalyticsLayoutStore } from "../../core/state/analytics-layout.store";
import { SurfaceCard } from "../../shared/ui/surface-card";
import { shouldSwap } from "./grid-sort";

const LAYOUT_LIST = "analytics-layout";

/** Evento di drop di una delle due liste (dati diversi, stesso handler): servono solo id delle liste e della card. */
type CardDrop = Pick<CdkDragDrop<unknown>, "previousIndex" | "currentIndex"> & {
  container: { id: string };
  previousContainer: { id: string };
  item: { data: AnalyticsCardId };
};

/**
 * Impostazioni → Analisi: scheletro a 6 colonne su cui trascinare le card visibili (ordine e larghezza),
 * più l'elenco di quelle nascoste. Le modifiche restano una bozza finché non si salva.
 */
@Component({
  selector: "app-analytics-layout-settings",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SurfaceCard, DragDropModule, MatButtonModule, MatButtonToggleModule, MatIconModule, MatTooltipModule],
  templateUrl: "./analytics-layout-settings.html",
  styleUrl: "./analytics-layout-settings.scss",
})
export class AnalyticsLayoutSettings {
  private readonly store = inject(AnalyticsLayoutStore);
  private readonly notifications = inject(NotificationService);

  protected readonly layoutList = LAYOUT_LIST;
  protected readonly span = WIDTH_SPAN;
  protected readonly widthLabel = WIDTH_LABEL;
  protected readonly allowedWidthsOf = allowedWidthsOf;
  protected readonly previewBars = [45, 70, 35, 85, 60, 50];
  protected readonly loaded = this.store.loaded;
  protected readonly saving = this.store.saving;

  /** bozza: si riallinea al layout salvato quando questo cambia (caricamento, salvataggio) */
  protected readonly draft = linkedSignal(() => this.store.layout().map((c) => ({ ...c })));
  protected readonly hidden = computed(() => {
    const visible = new Set(this.draft().map((c) => c.id));
    return ANALYTICS_CARDS.filter((c) => !visible.has(c.id));
  });
  protected readonly dirty = computed(() => !sameLayout(this.draft(), this.store.layout()));
  protected readonly isDefault = computed(() => sameLayout(this.draft(), DEFAULT_ANALYTICS_LAYOUT));

  /** ultima posizione del puntatore (coordinate viewport), letta dal predicato di riordino */
  private pointer = { x: 0, y: 0 };

  private readonly document = inject(DOCUMENT);

  constructor() {
    this.store.ensureLoaded();
    // stessi eventi del CDK, in cattura sul document e registrati prima dei suoi: la posizione è già aggiornata quando valuta il riordino
    const track = (e: MouseEvent | TouchEvent) => {
      const point = "touches" in e ? e.touches[0] : e;
      if (point) this.pointer = { x: point.clientX, y: point.clientY };
    };
    const events = ["mousemove", "touchmove"] as const;
    events.forEach((type) => this.document.addEventListener(type, track, { capture: true, passive: true }));
    inject(DestroyRef).onDestroy(() =>
      events.forEach((type) => this.document.removeEventListener(type, track, { capture: true })),
    );
  }

  /** Riordino nello scheletro: la card sotto il puntatore cede il posto solo oltre la sua metà (vedi `shouldSwap`). */
  // Lavora sul DOM e non sugli indici: per una card in arrivo dall'altra lista l'indice del CDK non corrisponde
  // a `getSortedItems()`, che contiene solo le card proprie. Con l'orientamento `mixed` il CDK sposta davvero
  // il segnaposto nel DOM, quindi l'ordine segnaposto/card dà la direzione dello spostamento.
  protected readonly sortPredicate = (_index: number, drag: CdkDrag, drop: CdkDropList): boolean => {
    const container = drop.element.nativeElement;
    const placeholder = drag.getPlaceholderElement();
    if (placeholder.parentElement !== container) return true; // ingresso dall'altra lista: decide il CDK
    const hit = this.document.elementFromPoint(this.pointer.x, this.pointer.y);
    const target = hit && Array.from(container.children).find((c) => c !== placeholder && c.contains(hit));
    if (!target) return true;
    const forward = Boolean(placeholder.compareDocumentPosition(target) & Node.DOCUMENT_POSITION_FOLLOWING);
    return shouldSwap(this.pointer, target.getBoundingClientRect(), placeholder.getBoundingClientRect(), forward);
  };

  /**
   * Durante il trascinamento il CDK misura le liste collegate una volta sola, all'inizio, e le rimisura solo a uno
   * scroll: se la griglia cambia altezza (il segnaposto cambia riga), l'elenco delle nascoste sotto di lei si sposta
   * ma resta "ricevibile" nella vecchia posizione e non accetta più la card. Uno scroll sintetico sul document (non
   * risale a window, quindi non tocca overlay & co.) gliela fa rimisurare; per la lista che sta trascinando è un no-op.
   */
  protected refreshDropZones() {
    this.document.dispatchEvent(new Event("scroll"));
  }

  protected cardOf(id: AnalyticsCardId): AnalyticsCardDefinition {
    return CARD_BY_ID.get(id)!;
  }

  /** Un unico handler per le due liste: riordino nello scheletro, aggiunta (da nascoste) e rimozione (verso nascoste). */
  protected drop(event: CardDrop) {
    const toLayout = event.container.id === LAYOUT_LIST;
    const fromLayout = event.previousContainer.id === LAYOUT_LIST;
    if (toLayout && fromLayout) {
      this.draft.update((cards) => {
        const next = [...cards];
        moveItemInArray(next, event.previousIndex, event.currentIndex);
        return next;
      });
    } else if (toLayout) {
      this.add(event.item.data, event.currentIndex);
    } else if (fromLayout) {
      this.remove(event.item.data);
    }
  }

  protected add(id: AnalyticsCardId, index = this.draft().length) {
    const card: PlacedAnalyticsCard = { id, width: this.cardOf(id).defaultWidth };
    this.draft.update((cards) => [...cards.slice(0, index), card, ...cards.slice(index)]);
  }

  protected remove(id: AnalyticsCardId) {
    this.draft.update((cards) => cards.filter((c) => c.id !== id));
  }

  protected setWidth(id: AnalyticsCardId, width: CardWidth | null) {
    if (!width) return;
    this.draft.update((cards) => cards.map((c) => (c.id === id ? { ...c, width } : c)));
  }

  protected resetToDefault() {
    this.draft.set(DEFAULT_ANALYTICS_LAYOUT.map((c) => ({ ...c })));
  }

  protected discard() {
    this.draft.set(this.store.layout().map((c) => ({ ...c })));
  }

  protected async save() {
    try {
      await this.store.save(this.draft());
      this.notifications.showSuccess("Layout della pagina Analisi salvato");
    } catch {
      // errore già mostrato dal layer API: la bozza resta per riprovare
    }
  }
}
