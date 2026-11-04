import { ChangeDetectionStrategy, Component, computed, input, output } from "@angular/core";
import { MatButton } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatSelectChange, MatSelectModule } from "@angular/material/select";

/**
 * Filtro a selezione multipla (conti, categorie).
 * Nessun filtro = tutte le opzioni spuntate: un click su una voce la toglie dalla selezione;
 * selezionare tutto o togliere l'ultima voce equivale a nessun filtro.
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
        [aria-label]="allLabel()"
        panelClass="multi-filter-panel"
      >
        <mat-select-trigger>{{ triggerLabel() }}</mat-select-trigger>
        <div class="panel-header">
          <span>{{ isAll() ? allLabel() : selected().length + " di " + options().length }}</span>
          <button matButton type="button" [disabled]="isAll()" (click)="selectionChange.emit([])">Seleziona tutti</button>
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
  `,
})
export class MultiSelectFilter {
  readonly icon = input.required<string>();
  readonly allLabel = input.required<string>();
  readonly options = input.required<{ id: number; name: string }[]>();
  readonly selected = input.required<number[]>();
  readonly selectionChange = output<number[]>();

  protected readonly isAll = computed(() => this.selected().length === 0 || this.selected().length === this.options().length);
  protected readonly checkedIds = computed(() => (this.isAll() ? this.options().map((o) => o.id) : this.selected()));
  protected readonly triggerLabel = computed(() => {
    if (this.isAll()) return this.allLabel();
    const selected = this.selected();
    const names = this.options()
      .filter((o) => selected.includes(o.id))
      .map((o) => o.name);
    return names.length <= 2 ? names.join(", ") : `${names.length} selezionati`;
  });

  protected onChange(event: MatSelectChange) {
    const ids = (event.value as number[]).filter(Number.isInteger);
    this.selectionChange.emit(ids.length === this.options().length ? [] : ids);
  }
}
