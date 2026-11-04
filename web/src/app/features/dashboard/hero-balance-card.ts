import { ChangeDetectionStrategy, Component, computed, inject, input, output } from "@angular/core";
import { MatIconButton } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltip } from "@angular/material/tooltip";
import { ThemeService } from "../../core/theme/theme.service";
import { formatCurrency, formatPercent, formatSignedCurrency } from "../../core/utils/format.utilities";
import { AmountPoint, NetWorthAreaChart } from "../../shared/charts/net-worth-area-chart";

/** Card principale: patrimonio attuale, variazione su 12 mesi e sparkline. */
@Component({
  selector: "app-hero-balance-card",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconButton, MatIconModule, MatTooltip, NetWorthAreaChart],
  template: `
    <!-- decorazione: cerchi sfumati, solo per lo stile Aurora -->
    @if (theme.tokens().presetId === "aurora") {
      <div class="glow" aria-hidden="true"></div>
    }

    <div class="head">
      <span class="label">Patrimonio netto</span>
      <button
        matIconButton
        class="eye"
        [matTooltip]="hidden() ? 'Mostra importi' : 'Nascondi importi'"
        [attr.aria-label]="hidden() ? 'Mostra importi' : 'Nascondi importi'"
        (click)="toggleHidden.emit()"
      >
        <mat-icon class="icon-sm icon-fill">{{ hidden() ? "visibility_off" : "visibility" }}</mat-icon>
      </button>
    </div>

    <p class="balance heading-font">{{ hidden() ? "•••••• €" : formatCurrency(balance()) }}</p>

    @if (delta() !== null) {
      <div class="delta-row">
        <span class="delta">
          <mat-icon class="icon-xs">{{ up() ? "trending_up" : "trending_down" }}</mat-icon>
          <span>
            {{ hidden() ? "•••" : formatSignedCurrency(delta()!) }}
            @if (deltaPct() !== null) {
              ({{ up() ? "+" : "−" }}{{ formatPercent(absPct()) }})
            }
          </span>
        </span>
        <span class="subtle">negli ultimi 12 mesi</span>
      </div>
    }

    <span class="spacer"></span>

    <div class="spark">
      <app-net-worth-area-chart [points]="series()" variant="spark" [height]="96" [color]="theme.tokens().hero.spark" />
    </div>

    <p class="foot">Media mensile dei saldi · aggiornato {{ asOfText() }}</p>
  `,
  styles: `
    :host {
      position: relative; overflow: hidden; display: flex; flex-direction: column; min-height: 260px; height: 100%;
      background: var(--app-hero-background); color: var(--app-hero-text); border: 1px solid var(--app-hero-border);
      border-radius: var(--app-radius-card); padding: 20px; animation: fadeUp .35s ease-out both;
    }
    @media (min-width: 900px) { :host { padding: 24px; } }
    .glow {
      position: absolute; right: -80px; top: -120px; width: 320px; height: 320px; border-radius: 50%;
      background: radial-gradient(circle, rgba(255,255,255,0.22) 0%, rgba(255,255,255,0) 70%);
    }
    .head { display: flex; align-items: center; justify-content: space-between; position: relative; }
    .label { font-size: 0.82rem; font-weight: 600; color: var(--app-hero-subtle); letter-spacing: 0.02em; }
    .eye { color: var(--app-hero-subtle); }
    .eye:hover { background-color: rgba(255,255,255,0.1); }
    .balance {
      position: relative; font-size: 2.1rem; font-weight: 750; letter-spacing: -0.03em; line-height: 1.1; margin-top: 8px;
    }
    @media (min-width: 900px) { .balance { font-size: 2.6rem; } }
    .delta-row { display: flex; align-items: center; gap: 8px; margin-top: 10px; position: relative; flex-wrap: wrap; }
    .delta {
      display: inline-flex; align-items: center; gap: 4px; padding: 2px 8px; border-radius: 999px;
      background-color: rgba(255,255,255,0.14); font-size: 0.78rem; font-weight: 650;
    }
    .subtle { font-size: 0.78rem; color: var(--app-hero-subtle); }
    .spacer { flex: 1; min-height: 24px; }
    .spark { margin: 0 -8px -8px; position: relative; }
    .foot { font-size: 0.7rem; color: var(--app-hero-subtle); margin-top: 8px; position: relative; }
  `,
})
export class HeroBalanceCard {
  readonly balance = input.required<number>();
  readonly series = input.required<AmountPoint[]>();
  readonly hidden = input.required<boolean>();
  readonly asOfText = input("");
  readonly toggleHidden = output<void>();

  protected readonly theme = inject(ThemeService);
  protected readonly formatCurrency = formatCurrency;
  protected readonly formatSignedCurrency = formatSignedCurrency;
  protected readonly formatPercent = formatPercent;

  private readonly first = computed(() => this.series()[0]?.amount);
  protected readonly delta = computed(() => {
    const first = this.first();
    return first !== undefined ? this.balance() - first : null;
  });
  protected readonly deltaPct = computed(() => {
    const first = this.first();
    const delta = this.delta();
    return first !== undefined && first !== 0 && delta !== null ? (delta / Math.abs(first)) * 100 : null;
  });
  protected readonly absPct = computed(() => Math.abs(this.deltaPct() ?? 0));
  protected readonly up = computed(() => (this.delta() ?? 0) >= 0);
}
