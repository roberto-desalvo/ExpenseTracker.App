import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, output, signal } from "@angular/core";
import { MatIconButton } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";

/** Campo di ricerca con debounce: emette undefined quando è vuoto, il testo da `minLength` caratteri. */
@Component({
  selector: "app-search-field",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatFormFieldModule, MatInputModule, MatIconModule, MatIconButton],
  template: `
    <mat-form-field appearance="outline" subscriptSizing="dynamic">
      <mat-icon matPrefix class="icon-sm prefix">search</mat-icon>
      <input matInput [placeholder]="placeholder()" [value]="value()" (input)="onInput(inputValue($event))" />
      @if (value()) {
        <button matIconButton matSuffix type="button" aria-label="Cancella ricerca" (click)="onInput('')">
          <mat-icon class="icon-sm">close</mat-icon>
        </button>
      }
      @if (tooShort()) {
        <mat-hint>Digita almeno {{ minLength() }} caratteri</mat-hint>
      }
    </mat-form-field>
  `,
  styles: `
    :host { display: block; width: 100%; }
    mat-form-field { width: 100%; }
    .prefix { margin-inline: 12px 4px; color: var(--app-text-muted); }
    @media (min-width: 600px) { :host { width: auto; min-width: 300px; } }
  `,
})
export class SearchField {
  readonly placeholder = input("");
  readonly minLength = input(3);
  readonly searchChange = output<string | undefined>();

  protected readonly value = signal("");
  protected readonly tooShort = computed(() => {
    const length = this.value().trim().length;
    return length > 0 && length < this.minLength();
  });

  private timeoutId: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.timeoutId));
  }

  protected inputValue(event: Event) {
    return (event.target as HTMLInputElement).value;
  }

  protected onInput(value: string) {
    this.value.set(value);
    clearTimeout(this.timeoutId);
    this.timeoutId = setTimeout(() => {
      const normalized = value.trim();
      if (normalized.length === 0) {
        this.searchChange.emit(undefined);
      } else if (normalized.length >= this.minLength()) {
        this.searchChange.emit(normalized);
      }
    }, 350);
  }
}
