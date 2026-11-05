import { ChangeDetectionStrategy, Component, input } from "@angular/core";

/** Titolo della pagina con sottotitolo e azioni (contenuto proiettato con l'attributo `actions`). */
@Component({
  selector: "app-page-header",
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="titles">
      @if (eyebrow()) {
        <span class="overline eyebrow">{{ eyebrow() }}</span>
      }
      <h1 class="heading-font">{{ title() }}</h1>
      @if (subtitle()) {
        <p class="subtitle">{{ subtitle() }}</p>
      }
    </div>
    <div class="actions"><ng-content select="[actions]" /></div>
  `,
  styles: `
    :host { display: flex; flex-direction: column; gap: 16px; }
    .titles { min-width: 0; }
    .eyebrow { display: block; margin-bottom: 2px; color: var(--app-text-secondary); font-size: var(--app-text-sm); }
    h1 { font-size: var(--app-text-lg); font-weight: 700; letter-spacing: -0.02em; line-height: 1.334; }
    .subtitle { color: var(--app-text-secondary); font-size: var(--app-text-md); margin-top: 4px; }
    .actions { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
    .actions:empty { display: none; }
    @media (min-width: 900px) {
      :host { flex-direction: row; align-items: flex-end; justify-content: space-between; }
      h1 { font-size: var(--app-text-xl); }
    }
  `,
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly subtitle = input<string | null | undefined>();
  readonly eyebrow = input<string | null | undefined>();
}
