import { booleanAttribute, ChangeDetectionStrategy, Component, input, output } from "@angular/core";

/** Logo (tre barre crescenti) e nome dell'app. */
@Component({
  selector: "app-brand-mark",
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { "[class.clickable]": "clickable()", "(click)": "activated.emit()" },
  template: `
    <span class="logo" aria-hidden="true">
      <svg width="18" height="18" viewBox="0 0 18 18" fill="none">
        <rect x="2" y="9" width="3" height="7" rx="1.2" fill="var(--app-hero-text)" opacity="0.55" />
        <rect x="7.5" y="5.5" width="3" height="10.5" rx="1.2" fill="var(--app-hero-text)" opacity="0.8" />
        <rect x="13" y="2" width="3" height="14" rx="1.2" fill="var(--app-hero-spark)" />
      </svg>
    </span>
    @if (!compact()) {
      <span class="texts">
        <span class="name">Expense Tracker</span>
        <span class="tagline">Finanze personali</span>
      </span>
    }
  `,
  styles: `
    :host { display: flex; align-items: center; gap: 12px; user-select: none; min-width: 0; }
    :host(.clickable) { cursor: pointer; }
    .logo {
      width: 34px; height: 34px; flex-shrink: 0; border-radius: var(--app-radius-avatar);
      background: var(--app-hero-background); border: 1px solid var(--app-hero-border);
      display: grid; place-items: center;
    }
    .texts { display: flex; flex-direction: column; min-width: 0; }
    .name {
      font-family: var(--app-heading-font-family); font-weight: 750; font-size: var(--app-text-md);
      letter-spacing: -0.015em; color: var(--app-text); line-height: 1.1; white-space: nowrap;
    }
    .tagline { font-size: var(--app-text-sm); color: var(--app-text-muted); line-height: 1.3; }
  `,
})
export class BrandMark {
  readonly compact = input(false, { transform: booleanAttribute });
  readonly clickable = input(false, { transform: booleanAttribute });
  readonly activated = output<void>();
}
