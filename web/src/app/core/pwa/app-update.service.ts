import { DestroyRef, Injectable, inject } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { MatSnackBar } from "@angular/material/snack-bar";
import { SwUpdate, VersionReadyEvent } from "@angular/service-worker";
import { filter, interval } from "rxjs";

/** Controllo nuove versioni ogni 30 minuti, oltre a quello fatto dal service worker all'avvio. */
const CHECK_INTERVAL_MS = 30 * 60 * 1000;

/**
 * PWA: quando il service worker ha scaricato una nuova versione dell'app propone di ricaricare
 * (l'app installata resta altrimenti sulla versione in cache fino alla riapertura).
 */
@Injectable({ providedIn: "root" })
export class AppUpdateService {
  private readonly swUpdate = inject(SwUpdate);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  watch() {
    if (!this.swUpdate.isEnabled) return;

    this.swUpdate.versionUpdates
      .pipe(
        filter((event): event is VersionReadyEvent => event.type === "VERSION_READY"),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.snackBar
          .open("È disponibile una nuova versione", "Aggiorna", {
            horizontalPosition: "right",
            verticalPosition: "bottom",
            panelClass: ["app-snack"],
          })
          .onAction()
          .subscribe(() => document.location.reload());
      });

    // versione non recuperabile (cache corrotta): ricarica per ripartire dal server
    this.swUpdate.unrecoverable.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => document.location.reload());

    interval(CHECK_INTERVAL_MS)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => void this.swUpdate.checkForUpdate().catch(() => undefined));
  }
}
