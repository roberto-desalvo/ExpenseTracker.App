import { ChangeDetectionStrategy, Component, inject, input } from "@angular/core";
import { MatButton } from "@angular/material/button";
import { MatDialogConfig } from "@angular/material/dialog";
import { MatIconModule } from "@angular/material/icon";
import { NotificationService } from "../../core/api/notification.service";
import { TemplateDialogHost } from "./template-dialog-host";

export type ConfirmAction = () => string | void | Promise<string | void>;

/** Conferma di eliminazione; `confirmAction` può restituire un messaggio di esito, mostrato come notifica. */
@Component({
  selector: "app-confirm-delete-dialog",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatIconModule],
  template: `
    <ng-template #dialogTpl>
      <div class="body">
        <span class="icon"><mat-icon>delete</mat-icon></span>
        <div>
          <h2>{{ title() }}</h2>
          <p>{{ message() }}</p>
        </div>
      </div>
      <div class="actions">
        <button matButton="outlined" type="button" [disabled]="busy()" (click)="requestClose()">{{ cancelLabel() }}</button>
        <button matButton="filled" type="button" class="btn-danger" [disabled]="busy()" (click)="confirm()">
          {{ busy() ? "Eliminazione..." : confirmLabel() }}
        </button>
      </div>
    </ng-template>
  `,
  styles: `
    .body { display: flex; gap: 16px; align-items: flex-start; padding: 24px 24px 8px; }
    .icon {
      width: 40px; height: 40px; flex-shrink: 0; border-radius: max(var(--app-radius-control), 8px);
      display: grid; place-items: center; background-color: var(--app-expense-soft); color: var(--app-expense);
    }
    h2 { font-weight: 650; font-size: 1.02rem; margin-bottom: 6px; color: var(--app-text); }
    p { font-size: 0.88rem; color: var(--app-text-secondary); }
    .actions { display: flex; justify-content: flex-end; gap: 8px; padding: 8px 24px 20px; }
  `,
})
export class ConfirmDeleteDialog extends TemplateDialogHost {
  readonly title = input("Conferma eliminazione");
  readonly message = input.required<string>();
  readonly confirmLabel = input("Elimina");
  readonly cancelLabel = input("Annulla");
  readonly confirmAction = input.required<ConfirmAction>();

  private readonly notifications = inject(NotificationService);

  protected dialogConfig(): MatDialogConfig {
    return { width: "100%", maxWidth: "444px", role: "alertdialog", ariaLabel: this.title() };
  }

  protected async confirm() {
    try {
      const result = await this.confirmAction()();
      if (typeof result === "string" && result) {
        this.notifications.showSuccess(result);
      }
      this.closed.emit();
    } catch {
      // resta aperta in caso di errore
    }
  }
}
