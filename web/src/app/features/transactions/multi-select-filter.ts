import { ChangeDetectionStrategy, Component, computed, input, output, signal } from "@angular/core";
import { MatButton } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatSelectChange, MatSelectModule } from "@angular/material/select";

/**
 * Filtro a selezione multipla (conti, categorie).
 * Nessun filtro = tutte le opzioni spuntate: un click su una voce la toglie dalla selezione;
 * selezionare tutto o togliere l'ultima voce equivale a nessun filtro.
 * "Deseleziona tutti" toglie tutte le spunte in un colpo, per poi scegliere solo le voci volute.
 */
@Component({
  selector: "app-multi-select-filter",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatFormFieldModule, MatSelectModule, MatIconModule, MatButton],
  template: `
    <mat-form-field class="field-block field-compact">
      <mat-icon matPrefix class="icon-sm prefix">{{ icon() }}</mat-icon>
      <mat-select
        multiple
        [value]="checkedIds()"
        (selectionChange)="onChange($event)"
        (openedChange)="!$event && cleared.set(false)"
        [aria-label]="allLabel()"
        panelClass="multi-filter-panel"
      >
        <mat-select-trigger>{{ triggerLabel() }}</mat-select-trigger>
        <div class="panel-header">
          <span>{{ headerLabel() }}</span>
          <span class="panel-actions">
            <button matButton type="button" [disabled]="checkedIds().length === 0" (click)="clearAll()">Deseleziona tutti</button>
            <button matButton type="button" [disabled]="isAll() && !cleared()" (click)="selectAll()">Seleziona tutti</button>
          </span>
        </div>
        @for (option of options(); track option.id) {
          <mat-option [value]="option.id">{{ option.name }}</mat-option>
        }
      </mat-select>
    </mat-form-field>
  `,
  styles: `
    :host { display: block; min-width: 0; flex: 1 1 100%; }
    @media (min-width: 600px) { :host { flex: 1 1 0; } }
    .prefix { margin-inline: 10px 6px; color: var(--app-text-muted); }
    .panel-header {
      position: sticky; top: -4px; z-index: 1; display: flex; align-items: center; justify-content: space-between;
      padding: 2px 6px 2px 12px; margin: -4px -4px 4px; background-color: var(--app-surface);
      border-bottom: 1px solid var(--app-border); font-size: 0.8rem; font-weight: 600; color: var(--app-text-secondary);
    }
    .panel-actions { display: flex; flex-wrap: wrap; justify-content: flex-end; }
  `,
})
export class MultiSelectFilter {
  readonly icon = input.required<string>();
  readonly allLabel = input.required<string>();
  readonly options = input.required<{ id: number; name: string }[]>();
  readonly selected = input.required<number[]>();
  readonly selectionChange = output<number[]>();

  /** "Deseleziona tutti" premuto: il pannello mostra zero spunte finché l'utente non sceglie una voce o lo chiude. */
  protected readonly cleared = signal(false);

  protected readonly isAll = computed(() => this.selected().length === 0 || this.selected().length === this.options().length);
  protected readonly checkedIds = computed(() => {
    if (this.cleared()) return [];
    return this.isAll() ? this.options().map((o) => o.id) : this.selected();
  });
  protected readonly headerLabel = computed(() => {
    if (this.cleared()) return `0 di ${this.options().length}`;
    return this.isAll() ? this.allLabel() : `${this.selected().length} di ${this.options().length}`;
  });
  protected readonly triggerLabel = computed(() => {
    if (this.isAll()) return this.allLabel();
    const selected = this.selected();
    const names = this.options()
      .filter((o) => selected.includes(o.id))
      .map((o) => o.name);
    return names.length <= 2 ? names.join(", ") : `${names.length} selezionati`;
  });

  protected selectAll() {
    this.cleared.set(false);
    this.selectionChange.emit([]);
  }

  protected clearAll() {
    this.cleared.set(true);
    // Nessuna voce scelta equivale a nessun filtro: se il pannello si chiude così, restano tutte.
    if (!this.isAll()) this.selectionChange.emit([]);
  }

  protected onChange(event: MatSelectChange) {
    this.cleared.set(false);
    const ids = (event.value as number[]).filter(Number.isInteger);
    this.selectionChange.emit(ids.length === this.options().length ? [] : ids);
  }
}
