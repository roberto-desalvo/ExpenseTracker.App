import { BreakpointObserver } from "@angular/cdk/layout";
import { ChangeDetectionStrategy, Component, computed, inject } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { MatIconModule } from "@angular/material/icon";
import { MatTabsModule } from "@angular/material/tabs";
import { ActivatedRoute, ParamMap, Router, RouterLink } from "@angular/router";
import { map } from "rxjs";
import { ROUTES } from "../../core/config/routes";
import { PageContainer } from "../../shared/ui/page-container";
import { PageHeader } from "../../shared/ui/page-header";
import { AccountsSettings } from "./accounts-settings";
import { AppearanceSettings } from "./appearance-settings";
import { CategoriesSettings } from "./categories-settings";

type SettingsSection = "categorie" | "conti" | "aspetto";

const SECTIONS: { id: SettingsSection; label: string; description: string; icon: string }[] = [
  { id: "categorie", label: "Categorie", description: "Classificazione dei movimenti e regole di import", icon: "category" },
  { id: "conti", label: "Conti", description: "Conti correnti, carte e wallet", icon: "account_balance_wallet" },
  { id: "aspetto", label: "Aspetto", description: "Stile, tema e navigazione", icon: "palette" },
];

/** Accetta anche il vecchio parametro `?tab=account|categorie`. */
const normalizeSection = (params: ParamMap): SettingsSection => {
  const value = params.get("sezione") ?? params.get("tab");
  if (value === "conti" || value === "account") return "conti";
  if (value === "aspetto") return "aspetto";
  return "categorie";
};

@Component({
  selector: "app-settings-page",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, PageContainer, PageHeader, MatTabsModule, MatIconModule, AccountsSettings, CategoriesSettings, AppearanceSettings],
  template: `
    <app-page-container>
      <app-page-header title="Impostazioni" subtitle="Gestisci le anagrafiche e personalizza l'aspetto dell'app." />

      @if (!isDesktop()) {
        <nav mat-tab-nav-bar [tabPanel]="panel" mat-stretch-tabs class="mobile-tabs">
          @for (s of sections; track s.id) {
            <a mat-tab-link [active]="s.id === active()" [routerLink]="[]" [queryParams]="queryFor(s.id)">
              <mat-icon class="icon-sm tab-icon">{{ s.icon }}</mat-icon>
              {{ s.label }}
            </a>
          }
        </nav>
      }

      <div class="layout">
        @if (isDesktop()) {
          <nav class="section-nav" aria-label="Sezioni impostazioni">
            @for (s of sections; track s.id) {
              <button
                type="button"
                class="section-item"
                [class.selected]="s.id === active()"
                [attr.aria-current]="s.id === active() ? 'page' : null"
                (click)="go(s.id)"
              >
                <span class="section-icon"><mat-icon class="icon-sm">{{ s.icon }}</mat-icon></span>
                <span class="col">
                  <span class="section-label">{{ s.label }}</span>
                  <span class="section-description">{{ s.description }}</span>
                </span>
              </button>
            }
          </nav>
        }

        <mat-tab-nav-panel #panel class="content">
          @if (isDesktop()) {
            <div class="content-head">
              <h2>{{ current().label }}</h2>
              <p class="text-secondary">{{ current().description }}</p>
            </div>
          }
          @switch (active()) {
            @case ("categorie") {
              <app-categories-settings />
            }
            @case ("conti") {
              <app-accounts-settings />
            }
            @case ("aspetto") {
              <app-appearance-settings />
            }
          }
        </mat-tab-nav-panel>
      </div>
    </app-page-container>
  `,
  styles: `
    .mobile-tabs { border-bottom: 1px solid var(--app-border); }
    .tab-icon { margin-right: 8px; }
    .layout { display: flex; gap: 24px; align-items: flex-start; }
    .section-nav { width: 260px; flex-shrink: 0; position: sticky; top: 24px; display: flex; flex-direction: column; gap: 4px; }
    .section-item {
      display: flex; gap: 12px; padding: 12px; text-align: left; cursor: pointer; font: inherit;
      border-radius: calc(var(--app-radius-control) + 2px); border: 1px solid transparent; background: transparent;
    }
    .section-item:hover { background-color: var(--app-surface-alt); }
    .section-item.selected { border-color: var(--app-border); background-color: var(--app-surface); box-shadow: var(--app-card-shadow); }
    .section-icon {
      width: 34px; height: 34px; flex-shrink: 0; border-radius: var(--app-radius-avatar); display: grid; place-items: center;
      background-color: var(--app-surface-alt); color: var(--app-text-secondary);
    }
    .selected .section-icon { background-color: var(--app-primary-soft); color: var(--app-primary); }
    .section-label { font-weight: 650; font-size: 0.9rem; color: var(--app-text); }
    .section-description { font-size: 0.74rem; color: var(--app-text-secondary); line-height: 1.35; }
    .content { flex: 1; min-width: 0; display: block; }
    .content-head { margin-bottom: 16px; }
    .content-head h2 { font-weight: 700; font-size: 1.15rem; }
    .content-head p { font-size: 0.84rem; }
  `,
})
export class SettingsPage {
  private readonly router = inject(Router);
  protected readonly sections = SECTIONS;

  protected readonly isDesktop = toSignal(
    inject(BreakpointObserver)
      .observe("(min-width: 900px)")
      .pipe(map((state) => state.matches)),
    { initialValue: window.matchMedia("(min-width: 900px)").matches },
  );

  protected readonly active = toSignal(inject(ActivatedRoute).queryParamMap.pipe(map(normalizeSection)), {
    initialValue: "categorie" as SettingsSection,
  });
  protected readonly current = computed(() => SECTIONS.find((s) => s.id === this.active()) ?? SECTIONS[0]);

  protected queryFor(section: SettingsSection) {
    return section === "categorie" ? {} : { sezione: section };
  }

  protected go(section: SettingsSection) {
    void this.router.navigate([ROUTES.settings], { queryParams: this.queryFor(section) });
  }
}
