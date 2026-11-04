import { ChangeDetectionStrategy, Component, input } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";

/** Stato vuoto: icona, titolo, testo e azione facoltativa (contenuto proiettato). */
@Component({
  selector: "app-empty-state",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule],
  host: { "[style.min-height.px]": "minHeight()" },
  template: `
    @if (icon()) {
      <span class="icon"><mat-icon>{{ icon() }}</mat-icon></span>
    }
    <p class="title">{{ title() }}</p>
    @if (text()) {
      <p class="text">{{ text() }}</p>
    }
    <ng-content />
  `,
  styles: `
    :host { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 10px; text-align: center; padding-inline: 16px; }
    .icon {
      width: 48px; height: 48px; border-radius: calc(var(--app-radius-control) + 4px);
      display: grid; place-items: center; background-color: var(--app-surface-alt); color: var(--app-text-secondary);
    }
    .icon .mat-icon { font-size: 24px; width: 24px; height: 24px; }
    .title { font-weight: 600; color: var(--app-text); font-size: 1rem; }
    .text { font-size: 0.85rem; color: var(--app-text-secondary); max-width: 360px; }
  `,
})
export class EmptyState {
  readonly icon = input<string>();
  readonly title = input.required<string>();
  readonly text = input<string>();
  readonly minHeight = input(220);
}
