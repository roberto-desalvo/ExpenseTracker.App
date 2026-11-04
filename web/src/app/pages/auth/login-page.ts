import { ChangeDetectionStrategy, Component, computed, inject, signal } from "@angular/core";
import { MatButton } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { AuthService } from "../../core/auth/auth.service";
import { BrandMark } from "../../shared/ui/brand-mark";

/** Pagina iniziale: accesso con l'account Microsoft (Entra ID) oppure avvio della demo. */
@Component({
  selector: "app-login-page",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatIconModule, MatProgressSpinner, BrandMark],
  template: `
    <div class="auth-card surface">
      <app-brand-mark />
      <div>
        <h1 class="heading-font">Accedi</h1>
        <p class="hint">Entra con il tuo account Microsoft per gestire i tuoi conti.</p>
      </div>
      @if (message()) {
        <div class="alert alert-error" role="alert">
          <mat-icon class="icon-sm">error</mat-icon>
          <span>{{ message() }}</span>
        </div>
      }
      <button matButton="filled" class="btn-lg btn-block" [disabled]="demoLoading()" (click)="auth.loginEntra()">
        Accedi con Microsoft
      </button>
      @if (auth.demoLoginEnabled) {
        <div class="divider"><span>oppure</span></div>
        <div>
          <button matButton="outlined" class="btn-lg btn-block" [disabled]="demoLoading()" (click)="startDemo()">
            @if (demoLoading()) {
              <mat-progress-spinner diameter="16" mode="indeterminate" class="spinner" />
            }
            Prova la demo
          </button>
          <p class="demo-hint">Esplora l'app con dati fittizi, senza registrazione.</p>
        </div>
      }
    </div>
  `,
  styleUrl: "./auth-pages.scss",
})
export class LoginPage {
  protected readonly auth = inject(AuthService);
  protected readonly demoLoading = signal(false);
  private readonly demoError = signal<string | null>(null);
  protected readonly message = computed(() => this.demoError() ?? this.auth.loginError());

  protected async startDemo() {
    this.demoError.set(null);
    this.demoLoading.set(true);
    try {
      await this.auth.loginDemo();
    } catch (error) {
      this.demoError.set(error instanceof Error ? error.message : "Avvio della demo non riuscito.");
      this.demoLoading.set(false);
    }
  }
}
