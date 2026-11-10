import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from "@angular/core";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { MatTooltip } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { NotificationService } from "../../core/api/notification.service";
import { ROUTES } from "../../core/config/routes";
import { OneDriveStore } from "../../core/state/onedrive.store";
import { ConfirmDeleteDialog } from "../../shared/ui/confirm-delete-dialog";
import { SurfaceCard } from "../../shared/ui/surface-card";

const dateTimeFormatter = new Intl.DateTimeFormat("it-IT", { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
const timeFormatter = new Intl.DateTimeFormat("it-IT", { hour: "2-digit", minute: "2-digit" });

/** Impostazioni → Import: collegamento del OneDrive personale per l'import automatico degli estratti conto. */
@Component({
  selector: "app-onedrive-settings",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SurfaceCard, ConfirmDeleteDialog, MatButton, MatIconButton, MatIconModule, MatProgressSpinner, MatTooltip, RouterLink],
  templateUrl: "./onedrive-settings.html",
  styleUrl: "./onedrive-settings.scss",
})
export class OneDriveSettings {
  protected readonly store = inject(OneDriveStore);
  private readonly notifications = inject(NotificationService);

  protected readonly routes = ROUTES;
  protected readonly disconnectOpen = signal(false);
  protected readonly disconnect = async () => {
    await this.store.disconnect();
  };

  constructor() {
    inject(DestroyRef).onDestroy(this.store.activate());
  }

  protected dateTime(value: string | null | undefined) {
    return value ? dateTimeFormatter.format(new Date(value)) : "—";
  }

  protected time(value: string | null | undefined) {
    return value ? timeFormatter.format(new Date(value)) : "—";
  }

  protected async copyCode(code: string) {
    try {
      await navigator.clipboard.writeText(code);
      this.notifications.showSuccess("Codice copiato");
    } catch {
      this.notifications.showError("Impossibile copiare il codice: selezionalo e copialo a mano");
    }
  }
}
