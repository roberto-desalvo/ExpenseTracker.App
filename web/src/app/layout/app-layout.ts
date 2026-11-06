import { BreakpointObserver } from "@angular/cdk/layout";
import { NgTemplateOutlet } from "@angular/common";
import { ChangeDetectionStrategy, Component, computed, inject, signal } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { MatIconButton } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltip } from "@angular/material/tooltip";
import { NavigationEnd, Router, RouterOutlet } from "@angular/router";
import { filter, map } from "rxjs";
import { AuthService } from "../core/auth/auth.service";
import { NAV_ITEMS, isNavItemActive } from "../core/config/navigation";
import { AccountTypesStore } from "../core/state/account-types.store";
import { CurrentUserStore } from "../core/state/current-user.store";
import { AccountsStore } from "../core/state/accounts.store";
import { CategoriesStore } from "../core/state/categories.store";
import { ThemeService } from "../core/theme/theme.service";
import { initialsOf } from "../core/utils/format.utilities";
import { TransactionModal } from "../features/transactions/transaction-modal";
import { TransferModal } from "../features/transactions/transfer-modal";
import { BrandMark } from "../shared/ui/brand-mark";
import { NewEntryButton, ThemeModeButton, UserMenuButton } from "./header-actions";

const COLLAPSED_STORAGE_KEY = "expense-tracker:sidebar-collapsed";

const readCollapsed = () => {
  try {
    return window.localStorage.getItem(COLLAPSED_STORAGE_KEY) === "true";
  } catch {
    return false;
  }
};

/**
 * Guscio dell'app: barra laterale (comprimibile) o barra superiore su desktop, menu a scomparsa su mobile.
 * Carica una volta l'anagrafica di conti e categorie e monta le modali di inserimento rapido.
 */
@Component({
  selector: "app-layout",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NgTemplateOutlet,
    RouterOutlet,
    MatIconModule,
    MatIconButton,
    MatTooltip,
    BrandMark,
    NewEntryButton,
    ThemeModeButton,
    UserMenuButton,
    TransactionModal,
    TransferModal,
  ],
  templateUrl: "./app-layout.html",
  styleUrl: "./app-layout.scss",
})
export class AppLayout {
  private readonly router = inject(Router);
  private readonly theme = inject(ThemeService);
  protected readonly auth = inject(AuthService);
  protected readonly navItems = NAV_ITEMS;

  protected readonly isDesktop = toSignal(
    inject(BreakpointObserver)
      .observe("(min-width: 900px)")
      .pipe(map((state) => state.matches)),
    { initialValue: window.matchMedia("(min-width: 900px)").matches },
  );

  private readonly pathname = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects.split(/[?#]/)[0]),
    ),
    { initialValue: this.router.url.split(/[?#]/)[0] },
  );

  protected readonly mobileOpen = signal(false);
  protected readonly collapsed = signal(readCollapsed());
  protected readonly useSidebar = computed(() => this.theme.preferences().nav === "sidebar" && this.isDesktop());
  protected readonly useTopbar = computed(() => this.theme.preferences().nav === "topbar" && this.isDesktop());
  protected readonly initials = computed(() => initialsOf(this.auth.userName()));

  constructor() {
    // anagrafiche condivise da tutte le pagine
    inject(AccountsStore).ensureLoaded();
    inject(CategoriesStore).ensureLoaded();
    inject(AccountTypesStore).ensureLoaded();
    void inject(CurrentUserStore).ensureLoaded();

    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => this.mobileOpen.set(false));
  }

  protected isActive(path: string) {
    const item = this.navItems.find((i) => i.path === path);
    return item ? isNavItemActive(item, this.pathname()) : false;
  }

  protected navigate(path: string) {
    void this.router.navigateByUrl(path);
  }

  protected toggleCollapsed() {
    this.collapsed.update((prev) => {
      try {
        window.localStorage.setItem(COLLAPSED_STORAGE_KEY, String(!prev));
      } catch {
        // ignora
      }
      return !prev;
    });
  }
}
