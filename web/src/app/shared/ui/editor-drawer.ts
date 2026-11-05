import { booleanAttribute, ChangeDetectionStrategy, Component, input, output } from "@angular/core";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatDialogConfig } from "@angular/material/dialog";
import { MatIconModule } from "@angular/material/icon";
import { TemplateDialogHost } from "./template-dialog-host";

/**
 * Pannello laterale per creare/modificare un elemento senza perdere di vista l'elenco.
 * Il contenuto con l'attributo `secondary-action` va a sinistra del footer (es. elimina).
 */
@Component({
  selector: "app-editor-drawer",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatIconButton, MatIconModule],
  template: `
    <ng-template #dialogTpl>
      <form class="drawer" (submit)="$event.preventDefault(); submitted.emit()">
        <div class="head">
          <div class="titles">
            <h2>{{ title() }}</h2>
            @if (subtitle()) {
              <p>{{ subtitle() }}</p>
            }
          </div>
          <button matIconButton type="button" class="close" aria-label="Chiudi" [disabled]="busy()" (click)="requestClose()">
            <mat-icon class="icon-sm">close</mat-icon>
          </button>
        </div>
        <div class="content"><ng-content /></div>
        <div class="footer">
          <ng-content select="[secondary-action]" />
          <span class="spacer"></span>
          <button matButton="outlined" type="button" [disabled]="busy()" (click)="requestClose()">Annulla</button>
          <button matButton="filled" type="submit" class="submit" [disabled]="busy() || submitDisabled()">
            {{ busy() ? "Salvataggio..." : submitLabel() }}
          </button>
        </div>
      </form>
    </ng-template>
  `,
  styles: `
    .drawer { display: flex; flex-direction: column; height: 100%; background-color: var(--app-surface); }
    .head { display: flex; align-items: flex-start; gap: 16px; padding: 20px 24px; border-bottom: 1px solid var(--app-border); }
    .titles { flex: 1; }
    h2 { font-weight: 700; font-size: var(--app-text-lg); color: var(--app-text); }
    p { font-size: var(--app-text-md); color: var(--app-text-secondary); margin-top: 2px; }
    .close { color: var(--app-text-muted); }
    .content { padding: 24px; flex: 1; overflow-y: auto; display: flex; flex-direction: column; gap: 20px; }
    .footer {
      display: flex; align-items: center; gap: 8px; padding: 16px 24px;
      border-top: 1px solid var(--app-border); background-color: var(--app-surface-alt);
    }
    .submit { min-width: 110px; }
  `,
})
export class EditorDrawer extends TemplateDialogHost {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
  readonly submitLabel = input("Salva");
  readonly submitDisabled = input(false, { transform: booleanAttribute });
  readonly submitted = output<void>();

  protected dialogConfig(): MatDialogConfig {
    return {
      position: { right: "0", top: "0" },
      height: "100%",
      width: "440px",
      maxWidth: "100vw",
      maxHeight: "100vh",
      panelClass: "app-drawer",
      autoFocus: "first-tabbable",
      ariaLabel: this.title(),
    };
  }
}
