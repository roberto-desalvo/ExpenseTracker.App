import { ChangeDetectionStrategy, Component, computed, input } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";

export type StatTone = "income" | "expense" | "neutral" | "accent" | "invested";

const TONE_VARS: Record<StatTone, { color: string; soft: string }> = {
  income: { color: "var(--app-income)", soft: "var(--app-income-soft)" },
  expense: { color: "var(--app-expense)", soft: "var(--app-expense-soft)" },
  neutral: { color: "var(--app-text-secondary)", soft: "var(--app-surface-alt)" },
  accent: { color: "var(--app-primary)", soft: "var(--app-primary-soft)" },
  invested: { color: "var(--app-invested)", soft: "var(--app-invested-soft)" },
};

/** Tile con un solo numero: quando la storia è un valore, il numero è il grafico. Nota a piè proiettata (`footnote`). */
@Component({
  selector: "app-stat-tile",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule],
  host: { "[style.--tone]": "toneVars().color", "[style.--tone-soft]": "toneVars().soft" },
  template: `
    <div class="head">
      <span class="icon"><mat-icon>{{ icon() }}</mat-icon></span>
      <span class="label">{{ label() }}</span>
    </div>
    <p class="value heading-font">{{ value() }}</p>
    @if (progress() !== undefined) {
      <div>
        <div class="track"><div class="bar" [style.width.%]="clampedProgress()"></div></div>
        @if (progressLabel()) {
          <p class="progress-label">{{ progressLabel() }}</p>
        }
      </div>
    }
    <div class="footnote"><ng-content /></div>
  `,
  styles: `
    :host {
      height: 100%; display: flex; flex-direction: column; gap: 12px; padding: 16px;
      background-color: var(--app-surface); border: 1px solid var(--app-border); border-radius: var(--app-radius-card);
      box-shadow: var(--app-card-shadow); animation: fadeUp .35s ease-out both; min-width: 0;
    }
    @media (min-width: 600px) { :host { padding: 20px; } }
    .head { display: flex; align-items: center; gap: 12px; }
    .icon {
      width: 34px; height: 34px; flex-shrink: 0; border-radius: var(--app-radius-avatar); display: grid; place-items: center;
      background-color: var(--tone-soft); color: var(--tone);
    }
    .icon .mat-icon { font-size: 20px; width: 19px; height: 19px; }
    .label { font-size: var(--app-text-md); font-weight: 600; color: var(--app-text-secondary); }
    .value { font-size: var(--app-text-lg); font-weight: 750; letter-spacing: -0.025em; color: var(--app-text); line-height: 1.15; }
    @media (min-width: 600px) { .value { font-size: var(--app-text-lg); } }
    @media (min-width: 900px) { .value { font-size: var(--app-text-xl); } }
    .track { height: 6px; border-radius: 3px; background-color: var(--app-surface-alt); overflow: hidden; }
    .bar { height: 100%; border-radius: 3px; background-color: var(--tone); transition: width .4s ease; }
    .progress-label { font-size: var(--app-text-sm); color: var(--app-text-muted); margin-top: 4px; }
    .footnote { margin-top: auto; font-size: var(--app-text-sm); color: var(--app-text-secondary); }
    .footnote:empty { display: none; }
  `,
})
export class StatTile {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  /** nome dell'icona Material Symbols */
  readonly icon = input.required<string>();
  readonly tone = input<StatTone>("neutral");
  /** 0-100: barra di avanzamento opzionale sotto il valore */
  readonly progress = input<number>();
  readonly progressLabel = input<string>();

  protected readonly toneVars = computed(() => TONE_VARS[this.tone()]);
  protected readonly clampedProgress = computed(() => Math.min(Math.max(this.progress() ?? 0, 0), 100));
}
