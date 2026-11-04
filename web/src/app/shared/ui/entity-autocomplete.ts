import { booleanAttribute, ChangeDetectionStrategy, Component, computed, effect, input, output, signal, untracked } from "@angular/core";
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from "@angular/material/autocomplete";
import { MatIconButton } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";

export interface EntityOption {
  id: number;
  label: string;
}

/** Selezione di un'entità (conto, categoria) con ricerca per testo; emette l'id scelto o null. */
@Component({
  selector: "app-entity-autocomplete",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatFormFieldModule, MatInputModule, MatAutocompleteModule, MatIconModule, MatIconButton],
  template: `
    <mat-form-field appearance="outline" class="field-block">
      <mat-label>{{ label() }}</mat-label>
      <input
        matInput
        [required]="required()"
        [disabled]="disabled()"
        [value]="text()"
        (input)="onType($event)"
        (blur)="onBlur()"
        [matAutocomplete]="auto"
      />
      @if (text() && !disabled()) {
        <button matIconButton matSuffix type="button" aria-label="Cancella" (click)="clear()">
          <mat-icon class="icon-sm">close</mat-icon>
        </button>
      }
      <mat-autocomplete #auto="matAutocomplete" (optionSelected)="onSelected($event)" [hideSingleSelectionIndicator]="false">
        @for (option of filtered(); track option.id) {
          <mat-option [value]="option.id">{{ option.label }}</mat-option>
        } @empty {
          <mat-option disabled>Nessuna opzione</mat-option>
        }
      </mat-autocomplete>
    </mat-form-field>
  `,
})
export class EntityAutocomplete {
  readonly label = input.required<string>();
  readonly options = input.required<EntityOption[]>();
  readonly value = input<number | null | undefined>(null);
  readonly required = input(false, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly valueChange = output<number | null>();

  protected readonly text = signal("");
  private readonly query = signal("");

  protected readonly filtered = computed(() => {
    const q = this.query().trim().toLowerCase();
    return q ? this.options().filter((o) => o.label.toLowerCase().includes(q)) : this.options();
  });

  private readonly selectedLabel = computed(() => this.options().find((o) => o.id === this.value())?.label ?? "");

  constructor() {
    // il testo mostrato segue il valore selezionato
    effect(() => {
      const label = this.selectedLabel();
      untracked(() => {
        this.text.set(label);
        this.query.set("");
      });
    });
  }

  protected onType(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.text.set(value);
    this.query.set(value);
  }

  protected onSelected(event: MatAutocompleteSelectedEvent) {
    const id = event.option.value as number;
    this.text.set(this.options().find((o) => o.id === id)?.label ?? "");
    this.query.set("");
    this.valueChange.emit(id);
  }

  /** testo non corrispondente a un'opzione: torna all'etichetta del valore selezionato */
  protected onBlur() {
    setTimeout(() => {
      if (this.text() !== this.selectedLabel()) {
        this.text.set(this.selectedLabel());
        this.query.set("");
      }
    }, 150);
  }

  protected clear() {
    this.text.set("");
    this.query.set("");
    this.valueChange.emit(null);
  }
}
