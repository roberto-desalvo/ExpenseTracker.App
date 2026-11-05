import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { MatIconRegistry } from "@angular/material/icon";
import { AuthService } from "./core/auth/auth.service";
import { AppUpdateService } from "./core/pwa/app-update.service";
import { ThemeService } from "./core/theme/theme.service";
import { AppLayout } from "./layout/app-layout";
import { DevLoginPage } from "./pages/auth/dev-login-page";
import { LoginPage } from "./pages/auth/login-page";

/**
 * Radice: applica il tema e richiede un utente autenticato prima di mostrare l'app
 * (login Entra ID o demo in produzione, solo nome utente in sviluppo con il bypass).
 * L'URL richiesto resta invariato: dopo il login si atterra sulla pagina di partenza.
 */
@Component({
  selector: "app-root",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AppLayout, LoginPage, DevLoginPage],
  template: `
    @if (auth.isAuthenticated()) {
      <app-layout />
    } @else if (auth.devBypass) {
      <app-dev-login-page />
    } @else {
      <app-login-page />
    }
  `,
})
export class App {
  protected readonly auth = inject(AuthService);

  constructor() {
    // istanziato subito: applica i token dello stile attivo anche alla pagina di login
    inject(ThemeService);
    inject(AppUpdateService).watch();
    inject(MatIconRegistry).setDefaultFontSetClass("material-symbols-rounded");
  }
}
