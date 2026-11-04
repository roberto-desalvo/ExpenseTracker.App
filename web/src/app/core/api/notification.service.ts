import { Injectable, inject } from "@angular/core";
import { MatSnackBar } from "@angular/material/snack-bar";

/** Messaggi a scomparsa in basso a destra: errori (anche quelli delle API) ed esiti positivi. */
@Injectable({ providedIn: "root" })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  showError(message: string) {
    this.snackBar.open(message || "Si è verificato un errore", "Chiudi", {
      duration: 5000,
      horizontalPosition: "right",
      verticalPosition: "bottom",
      panelClass: ["app-snack", "app-snack--error"],
    });
  }

  showSuccess(message: string) {
    this.snackBar.open(message || "Operazione completata", "Chiudi", {
      duration: 4000,
      horizontalPosition: "right",
      verticalPosition: "bottom",
      panelClass: ["app-snack", "app-snack--success"],
    });
  }
}
