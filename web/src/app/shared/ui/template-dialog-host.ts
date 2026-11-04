import { booleanAttribute, Directive, TemplateRef, effect, inject, input, output, untracked, viewChild } from "@angular/core";
import { MatDialog, MatDialogConfig, MatDialogRef } from "@angular/material/dialog";

/**
 * Base dei pannelli controllati da un input `open` (modale, drawer, conferma):
 * apre il template `#dialogTpl` in un MatDialog finché `open` è true, blocca la chiusura
 * mentre `busy` è true ed emette `closed` quando l'utente chiude (Esc, sfondo, Annulla).
 */
@Directive()
export abstract class TemplateDialogHost {
  readonly open = input(false, { transform: booleanAttribute });
  readonly busy = input(false, { transform: booleanAttribute });
  readonly closed = output<void>();

  private readonly dialog = inject(MatDialog);
  private readonly template = viewChild.required<TemplateRef<unknown>>("dialogTpl");
  private ref: MatDialogRef<unknown> | null = null;

  protected abstract dialogConfig(): MatDialogConfig;

  constructor() {
    effect(() => {
      const open = this.open();
      untracked(() => (open ? this.show() : this.hide()));
    });

    effect(() => {
      const busy = this.busy();
      if (this.ref) this.ref.disableClose = busy;
    });
  }

  /** Richiesta di chiusura dall'interno (es. bottone Annulla). */
  protected requestClose() {
    if (this.busy()) return;
    this.closed.emit();
  }

  private show() {
    if (this.ref) return;
    const ref = this.dialog.open(this.template(), { ...this.dialogConfig(), disableClose: this.busy() });
    this.ref = ref;
    ref.afterClosed().subscribe(() => {
      // chiuso dall'utente (Esc o clic sullo sfondo) e non dall'input: notifica il genitore
      if (this.ref === ref) {
        this.ref = null;
        this.closed.emit();
      }
    });
  }

  private hide() {
    const ref = this.ref;
    if (!ref) return;
    this.ref = null;
    ref.close();
  }
}
