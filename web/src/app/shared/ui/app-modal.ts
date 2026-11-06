import { ChangeDetectionStrategy, Component, inject, input } from "@angular/core";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatDialogConfig } from "@angular/material/dialog";
import { MatIconModule } from "@angular/material/icon";
import { NotificationService } from "../../core/api/notification.service";
import { TemplateDialogHost } from "./template-dialog-host";

export type ModalSubmitAction = () => string | void | Promise<string | void>;

/**
 * Modale con form: intestazione, campi (contenuto proiettato), azioni. A schermo intero su mobile.
 * `submitAction` può restituire un messaggio di esito, mostrato come notifica; in caso di errore la modale resta aperta.
 */
@Component({
  selector: "app-modal",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatIconButton, MatIconModule],
  template: `
    <ng-template #dialogTpl>
      <form class="modal" (submit)="onSubmit($event)">
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
        <div class="actions">
          <button matButton="outlined" type="button" [disabled]="busy()" (click)="requestClose()">{{ cancelLabel() }}</button>
          <button
            matButton="filled"
            type="submit"
            class="submit"
            [class.btn-danger]="submitVariant() === 'error'"
            [disabled]="busy()"
          >
            {{ busy() ? "Attendere..." : submitLabel() }}
          </button>
        </div>
      </form>
    </ng-template>
  `,
  styles: `
    /* altezza limitata alla finestra: scorre solo il contenuto, intestazione e azioni restano visibili */
    .modal { display: flex; flex-direction: column; max-height: calc(100dvh - 48px); }
    @media (max-width: 599.98px) {
      .modal { max-height: 100dvh; }
    }
    .head { display: flex; align-items: flex-start; gap: 16px; padding: 24px 24px 8px; }
    .titles { flex: 1; }
    h2 { font-weight: 700; font-size: var(--app-text-lg); letter-spacing: -0.01em; color: var(--app-text); }
    p { font-size: var(--app-text-md); color: var(--app-text-secondary); margin-top: 2px; }
    .close { color: var(--app-text-muted); margin: -4px -8px 0 0; }
    .content { padding: 12px 24px 8px; display: flex; flex-direction: column; gap: 16px; overflow-y: auto; flex: 1; min-height: 0; }
    .actions {
      display: flex; justify-content: flex-end; gap: 8px; padding: 16px 24px;
      border-top: 1px solid var(--app-border); margin-top: 8px;
    }
    .submit { min-width: 120px; }
  `,
})
export class AppModal extends TemplateDialogHost {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
  readonly submitLabel = input("Salva");
  readonly cancelLabel = input("Annulla");
  readonly submitVariant = input<"primary" | "error">("primary");
  readonly maxWidth = input(480);
  readonly submitAction = input.required<ModalSubmitAction>();

  private readonly notifications = inject(NotificationService);

  protected dialogConfig(): MatDialogConfig {
    return {
      width: "100%",
      maxWidth: `${this.maxWidth()}px`,
      panelClass: "app-dialog-fullscreen-mobile",
      autoFocus: "first-tabbable",
      ariaLabel: this.title(),
    };
  }

  protected async onSubmit(event: Event) {
    event.preventDefault();
    try {
      const message = await this.submitAction()();
      if (typeof message === "string" && message) {
        this.notifications.showSuccess(message);
      }
      this.closed.emit();
    } catch {
      // resta aperta in caso di errore: lo mostra il layer API
    }
  }
}
