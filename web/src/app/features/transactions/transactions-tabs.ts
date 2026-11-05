import { ChangeDetectionStrategy, Component, computed, inject, input } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";
import { MatTabsModule } from "@angular/material/tabs";
import { RouterLink } from "@angular/router";
import { ROUTES } from "../../core/config/routes";
import { ImportsStore } from "../../core/state/imports.store";

type TransactionsTab = "movimenti" | "import";

/**
 * Tab della sezione Movimenti: elenco dei movimenti e storico degli import da file.
 * Il tab Import compare se l'utente ha conti importabili oppure se è già aperto (es. link diretto).
 */
@Component({
  selector: "app-transactions-tabs",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatTabsModule, MatIconModule, RouterLink],
  template: `
    @if (visible()) {
      <nav mat-tab-nav-bar [tabPanel]="panel" class="tabs" aria-label="Sezioni movimenti">
        <a mat-tab-link [active]="active() === 'movimenti'" [routerLink]="routes.transactions">
          <mat-icon class="icon-sm tab-icon">receipt_long</mat-icon>
          Movimenti
        </a>
        <a mat-tab-link [active]="active() === 'import'" [routerLink]="routes.imports">
          <mat-icon class="icon-sm tab-icon">upload_file</mat-icon>
          Import
          @if (imports.activeCount() > 0) {
            <span class="badge">{{ imports.activeCount() }}</span>
          }
        </a>
      </nav>
      <mat-tab-nav-panel #panel />
    }
  `,
  styles: `
    :host { display: block; }
    .tabs { border-bottom: 1px solid var(--app-border); }
    .tab-icon { margin-right: 8px; }
    .badge {
      margin-left: 8px; min-width: 20px; height: 20px; padding: 0 6px; border-radius: 10px;
      display: inline-grid; place-items: center; font-size: 0.72rem; font-weight: 700;
      background-color: var(--app-primary-soft); color: var(--app-primary);
    }
  `,
})
export class TransactionsTabs {
  readonly active = input.required<TransactionsTab>();

  protected readonly imports = inject(ImportsStore);
  protected readonly routes = ROUTES;
  protected readonly visible = computed(() => this.imports.canImport() || this.active() === "import");
}
