import { booleanAttribute, ChangeDetectionStrategy, Component, computed, input } from "@angular/core";

/**
 * Contenitore base di tutte le sezioni: superficie, bordo, raggio e ombra dello stile attivo.
 * Il contenuto con l'attributo `card-action` va a destra dell'intestazione (impostare `hasAction`).
 */
@Component({
  selector: "app-surface-card",
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: "surface", "[class.flush]": "flush()", "[style.--card-pad.px]": "padding()" },
  template: `
    @if (hasHeader()) {
      <div class="header">
        <div class="titles">
          @if (title()) {
            <h2>{{ title() }}</h2>
          }
          @if (subtitle()) {
            <p>{{ subtitle() }}</p>
          }
        </div>
        @if (hasAction()) {
          <div class="action"><ng-content select="[card-action]" /></div>
        }
      </div>
    }
    <div class="body" [class.with-header]="hasHeader()"><ng-content /></div>
  `,
  styles: `
    :host { display: flex; flex-direction: column; min-width: 0; animation: fadeUp .35s ease-out both; }
    .header {
      display: flex; flex-wrap: wrap; align-items: flex-start; justify-content: space-between; gap: 12px;
      padding: var(--card-pad) var(--card-pad) 0;
    }
    :host(.flush) .header { padding-bottom: 12px; }
    .titles { min-width: 0; flex: 1 1 160px; }
    h2 { font-size: 0.98rem; font-weight: 650; color: var(--app-text); line-height: 1.35; }
    p { font-size: 0.8rem; color: var(--app-text-secondary); margin-top: 2px; }
    .action { flex-shrink: 0; }
    .body { padding: var(--card-pad); flex: 1; min-width: 0; }
    .body.with-header { padding-top: 16px; }
    :host(.flush) .body { padding: 0; }
  `,
})
export class SurfaceCard {
  readonly title = input<string | null | undefined>();
  readonly subtitle = input<string | null | undefined>();
  readonly flush = input(false, { transform: booleanAttribute });
  /** padding in px */
  readonly padding = input(20);
  /** true se viene proiettato contenuto `card-action` */
  readonly hasAction = input(false, { transform: booleanAttribute });

  protected readonly hasHeader = computed(() => Boolean(this.title() || this.subtitle() || this.hasAction()));
}
