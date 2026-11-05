import { ChangeDetectionStrategy, Component, inject, signal } from "@angular/core";
import { MatButton } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { ImportsStore } from "../../core/state/imports.store";
import { ImportModal } from "./import-modal";

/** Bottone "Importa" con la sua modale; visibile solo se l'utente ha conti di un tipo importabile. */
@Component({
  selector: "app-import-button",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatIconModule, ImportModal],
  template: `
    @if (imports.canImport()) {
      <button matButton="outlined" (click)="open.set(true)"><mat-icon>upload_file</mat-icon> Importa</button>
      <app-import-modal [open]="open()" (closed)="open.set(false)" />
    }
  `,
  styles: `
    :host { display: contents; }
  `,
})
export class ImportButton {
  protected readonly imports = inject(ImportsStore);
  protected readonly open = signal(false);
}
