import { ChangeDetectionStrategy, Component } from "@angular/core";

/** Larghezza massima e spaziature comuni a tutte le pagine. */
@Component({
  selector: "app-page-container",
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<ng-content />`,
  styles: `
    :host {
      flex: 1; width: 100%; max-width: 1440px; margin-inline: auto; min-width: 0;
      display: flex; flex-direction: column; gap: 16px;
      padding: 20px 16px 48px;
    }
    @media (min-width: 900px) {
      :host { gap: 20px; padding: 28px 28px 48px; }
    }
  `,
})
export class PageContainer {}
