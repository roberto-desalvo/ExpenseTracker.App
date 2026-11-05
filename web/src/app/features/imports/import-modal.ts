import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal, untracked } from "@angular/core";
import { MatButton } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatSelectModule } from "@angular/material/select";
import { AccountTypesStore } from "../../core/state/account-types.store";
import { ImportsStore } from "../../core/state/imports.store";
import { AppModal } from "../../shared/ui/app-modal";

/** Deve coincidere con il limite del BE (IImportJobService.MaxFileSizeBytes). */
const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;
const ALLOWED_EXTENSIONS = [".csv", ".xlsx"];

const extensionOf = (name: string) => {
  const dot = name.lastIndexOf(".");
  return dot >= 0 ? name.slice(dot).toLowerCase() : "";
};

/** Caricamento di un estratto conto (CSV o XLSX esportato dalla banca) su uno dei conti importabili. */
@Component({
  selector: "app-import-modal",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AppModal, MatFormFieldModule, MatSelectModule, MatIconModule, MatButton],
  template: `
    <app-modal
      [open]="open()"
      (closed)="closed.emit()"
      title="Importa movimenti"
      subtitle="Carica l'estratto conto esportato dalla banca"
      [submitAction]="submit"
      [busy]="busy()"
      submitLabel="Importa"
    >
      <mat-form-field appearance="outline" class="field-block" subscriptSizing="dynamic">
        <mat-label>Conto</mat-label>
        <mat-select [value]="accountId()" (valueChange)="accountId.set($event)" [disabled]="busy()" required>
          @for (account of imports.importableAccounts(); track account.id) {
            <mat-option [value]="account.id">
              {{ account.name }}
              @if (typeName(account.accountTypeId); as type) {
                <span class="option-type">· {{ type }}</span>
              }
            </mat-option>
          }
        </mat-select>
      </mat-form-field>

      <input #fileInput type="file" class="hidden-input" [accept]="accept" (change)="pick($event)" />
      <div class="file-picker" [class.selected]="file() !== null">
        <mat-icon class="file-icon">{{ file() ? "description" : "upload_file" }}</mat-icon>
        <div class="col spacer file-text">
          <span class="file-name nowrap">{{ file()?.name ?? "Nessun file selezionato" }}</span>
          <span class="file-hint">{{ file() ? fileSize() : "CSV o Excel (.xlsx), fino a 10 MB" }}</span>
        </div>
        <button matButton="outlined" type="button" [disabled]="busy()" (click)="fileInput.click()">
          {{ file() ? "Cambia" : "Scegli file" }}
        </button>
      </div>

      @if (attempted() && validationError(); as error) {
        <div class="alert alert-warning">
          <mat-icon class="icon-sm">warning</mat-icon>
          <span>{{ error }}</span>
        </div>
      }

      <p class="explain">
        Il file viene elaborato in background: puoi continuare a usare l'app e seguire l'avanzamento nella sezione
        Import di Movimenti. I movimenti già importati vengono riconosciuti e saltati.
      </p>
    </app-modal>
  `,
  styles: `
    .hidden-input { display: none; }
    .option-type { color: var(--app-text-muted); font-size: 0.8rem; }
    .file-picker {
      display: flex; align-items: center; gap: 12px; padding: 12px;
      border: 1px dashed var(--app-border-strong); border-radius: var(--app-radius-control);
    }
    .file-picker.selected { border-style: solid; border-color: var(--app-border); background-color: var(--app-surface-alt); }
    .file-icon { color: var(--app-text-secondary); flex-shrink: 0; }
    .file-text { min-width: 0; }
    .file-name { font-weight: 600; font-size: 0.88rem; color: var(--app-text); }
    .file-hint { font-size: 0.76rem; color: var(--app-text-secondary); }
    .explain { font-size: 0.82rem; color: var(--app-text-secondary); }
  `,
})
export class ImportModal {
  readonly open = input(false);
  readonly closed = output<void>();

  protected readonly imports = inject(ImportsStore);
  private readonly accountTypes = inject(AccountTypesStore);

  protected readonly accept = ALLOWED_EXTENSIONS.join(",");
  protected readonly accountId = signal<number | null>(null);
  protected readonly file = signal<File | null>(null);
  protected readonly busy = signal(false);
  protected readonly attempted = signal(false);

  protected readonly fileSize = computed(() => {
    const size = this.file()?.size ?? 0;
    return size >= 1024 * 1024 ? `${(size / (1024 * 1024)).toFixed(1)} MB` : `${Math.max(1, Math.round(size / 1024))} KB`;
  });

  protected readonly validationError = computed(() => {
    const file = this.file();
    if (this.accountId() === null) return "Scegli il conto su cui importare i movimenti.";
    if (!file) return "Scegli il file da importare.";
    if (!ALLOWED_EXTENSIONS.includes(extensionOf(file.name))) return "Formato non supportato: usa un file .csv o .xlsx.";
    if (file.size === 0) return "Il file è vuoto.";
    if (file.size > MAX_FILE_SIZE_BYTES) return "Il file supera la dimensione massima di 10 MB.";
    return null;
  });

  constructor() {
    effect(() => {
      if (!this.open()) return;
      untracked(() => {
        // un solo conto importabile: preselezionato
        const accounts = this.imports.importableAccounts();
        this.accountId.set(accounts.length === 1 ? accounts[0].id : null);
        this.file.set(null);
        this.attempted.set(false);
      });
    });
  }

  protected typeName(accountTypeId: number | null | undefined) {
    return accountTypeId == null ? null : (this.accountTypes.byId().get(accountTypeId)?.name ?? null);
  }

  protected pick(event: Event) {
    const input = event.target as HTMLInputElement;
    this.file.set(input.files?.[0] ?? null);
    // permette di riselezionare lo stesso file dopo averlo cambiato
    input.value = "";
  }

  protected readonly submit = async (): Promise<string> => {
    this.attempted.set(true);
    const error = this.validationError();
    const accountId = this.accountId();
    const file = this.file();
    if (error || accountId === null || !file) throw new Error(error ?? "Dati mancanti");

    this.busy.set(true);
    try {
      await this.imports.upload(accountId, file);
      return `“${file.name}” in coda di elaborazione`;
    } finally {
      this.busy.set(false);
    }
  };
}
