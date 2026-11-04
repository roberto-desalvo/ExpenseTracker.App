import { ChangeDetectionStrategy, Component, computed, inject, signal } from "@angular/core";
import { MatButton } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { AuthService } from "../../core/auth/auth.service";
import { DEV_USER_MAX_LENGTH } from "../../core/auth/devAuth";
import { BrandMark } from "../../shared/ui/brand-mark";

/** Login di sviluppo: chiede solo il nome utente (autenticazione Entra ID bypassata). */
@Component({
  selector: "app-dev-login-page",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatFormFieldModule, MatInputModule, MatIconModule, BrandMark],
  template: `
    <form class="auth-card surface" (submit)="submit($event)">
      <app-brand-mark />
      <div>
        <h1 class="heading-font">Accedi</h1>
        <p class="hint">Indica con quale utente vuoi entrare.</p>
      </div>
      <div class="alert alert-info">
        <mat-icon class="icon-sm">info</mat-icon>
        <span>Modalità sviluppo: autenticazione Entra ID disattivata.</span>
      </div>
      <mat-form-field appearance="outline" class="field-block" subscriptSizing="dynamic">
        <mat-label>Nome utente</mat-label>
        <input matInput [maxLength]="maxLength" [value]="userName()" (input)="onInput($event)" />
      </mat-form-field>
      <button matButton="filled" type="submit" class="btn-lg btn-block" [disabled]="!trimmed()">Accedi</button>
    </form>
  `,
  styleUrl: "./auth-pages.scss",
})
export class DevLoginPage {
  private readonly auth = inject(AuthService);
  protected readonly maxLength = DEV_USER_MAX_LENGTH;
  protected readonly userName = signal("");
  protected readonly trimmed = computed(() => this.userName().trim());

  protected onInput(event: Event) {
    this.userName.set((event.target as HTMLInputElement).value);
  }

  protected submit(event: Event) {
    event.preventDefault();
    const name = this.trimmed();
    if (name) this.auth.loginDev(name);
  }
}
