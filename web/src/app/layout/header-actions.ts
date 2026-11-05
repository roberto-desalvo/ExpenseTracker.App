import { booleanAttribute, ChangeDetectionStrategy, Component, computed, inject, input } from "@angular/core";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatDividerModule } from "@angular/material/divider";
import { MatIconModule } from "@angular/material/icon";
import { MatMenuModule } from "@angular/material/menu";
import { MatTooltip } from "@angular/material/tooltip";
import { Router } from "@angular/router";
import { AuthService } from "../core/auth/auth.service";
import { ROUTES } from "../core/config/routes";
import { QuickActionsService } from "../core/state/quick-actions.service";
import { ThemeService } from "../core/theme/theme.service";
import { initialsOf } from "../core/utils/format.utilities";

/** Bottone "Nuovo" con menu: transazione o trasferimento. */
@Component({
  selector: "app-new-entry-button",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatIconButton, MatIconModule, MatMenuModule, MatTooltip],
  host: { "[class.full-width]": "fullWidth()" },
  template: `
    @if (iconOnly()) {
      <button
        matIconButton
        class="icon-btn-primary"
        aria-label="Nuovo movimento"
        matTooltip="Nuovo movimento"
        [matMenuTriggerFor]="menu"
      >
        <mat-icon>add</mat-icon>
      </button>
    } @else {
      <button matButton="filled" [class.btn-block]="fullWidth()" [matMenuTriggerFor]="menu">
        <mat-icon>add</mat-icon> Nuovo
      </button>
    }
    <mat-menu #menu="matMenu" xPosition="before" class="menu-wide">
      <button mat-menu-item (click)="quickActions.openNewTransaction()">
        <mat-icon>receipt_long</mat-icon>
        <span class="menu-item-two-lines">
          <span>Transazione</span>
          <span class="menu-item-secondary">Entrata o uscita su un conto</span>
        </span>
      </button>
      <button mat-menu-item (click)="quickActions.openNewTransfer()">
        <mat-icon>compare_arrows</mat-icon>
        <span class="menu-item-two-lines">
          <span>Trasferimento</span>
          <span class="menu-item-secondary">Spostamento tra due conti</span>
        </span>
      </button>
    </mat-menu>
  `,
  styles: `
    :host { display: inline-flex; }
    :host(.full-width) { display: flex; width: 100%; }
  `,
})
export class NewEntryButton {
  readonly iconOnly = input(false, { transform: booleanAttribute });
  readonly fullWidth = input(false, { transform: booleanAttribute });
  protected readonly quickActions = inject(QuickActionsService);
}

@Component({
  selector: "app-theme-mode-button",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconButton, MatIconModule, MatTooltip],
  template: `
    <button
      matIconButton
      class="muted"
      aria-label="Cambia tema"
      [matTooltip]="isDark() ? 'Passa al tema chiaro' : 'Passa al tema scuro'"
      (click)="theme.toggleTheme()"
    >
      <mat-icon class="icon-sm">{{ isDark() ? "light_mode" : "dark_mode" }}</mat-icon>
    </button>
  `,
})
export class ThemeModeButton {
  protected readonly theme = inject(ThemeService);
  protected readonly isDark = computed(() => this.theme.mode() === "dark");
}

@Component({
  selector: "app-user-menu-button",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconButton, MatIconModule, MatMenuModule, MatTooltip, MatDividerModule],
  template: `
    <button matIconButton aria-label="Menu utente" [matTooltip]="auth.userName() || 'Account'" [matMenuTriggerFor]="menu">
      <span class="initials avatar">{{ initials() }}</span>
    </button>
    <mat-menu #menu="matMenu" xPosition="before">
      <div class="who">
        <span class="text-muted label">Connesso come</span>
        <span class="name nowrap">{{ auth.userName() || "Utente" }}</span>
      </div>
      <mat-divider />
      <button mat-menu-item (click)="openAppearance()">
        <mat-icon>palette</mat-icon>
        <span>Aspetto</span>
      </button>
      <button mat-menu-item class="mat-mdc-menu-item-danger" (click)="auth.logout()">
        <mat-icon>logout</mat-icon>
        <span>Esci</span>
      </button>
    </mat-menu>
  `,
  styles: `
    .avatar { width: 32px; height: 32px; font-size: var(--app-text-sm); }
    .who { padding: 8px 12px; min-width: 200px; display: flex; flex-direction: column; }
    .label { font-size: var(--app-text-sm); }
    .name { font-weight: 600; font-size: var(--app-text-md); }
  `,
})
export class UserMenuButton {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly initials = computed(() => initialsOf(this.auth.userName()));

  protected openAppearance() {
    void this.router.navigate([ROUTES.settings], { queryParams: { sezione: "aspetto" } });
  }
}
