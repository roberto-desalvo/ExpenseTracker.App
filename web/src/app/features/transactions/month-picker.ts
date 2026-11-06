import { ChangeDetectionStrategy, Component, computed, input, output } from "@angular/core";
import { MAT_DATE_FORMATS, MatDateFormats } from "@angular/material/core";
import { MatDatepicker, MatDatepickerModule } from "@angular/material/datepicker";
import { format, parseISO } from "date-fns";
import { TransactionMonthOption } from "../../core/models/TransactionMonthOption";

// il campo mostra solo mese e anno; il calendario parte dalla vista degli anni
const MONTH_YEAR_FORMATS: MatDateFormats = {
  parse: { dateInput: "MM/yyyy" },
  display: {
    dateInput: "MMMM yyyy",
    monthYearLabel: "MMM yyyy",
    dateA11yLabel: "MMMM yyyy",
    monthYearA11yLabel: "MMMM yyyy",
  },
};

const monthKey = (date: Date) => format(date, "yyyy-MM");

/**
 * Selettore di mese: calendario Material che si apre sugli anni e si chiude alla scelta del mese.
 * Sono selezionabili solo i mesi presenti in `months` (quelli con movimenti); emette lo `startDate` del mese scelto.
 */
@Component({
  selector: "app-month-picker",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatDatepickerModule],
  providers: [{ provide: MAT_DATE_FORMATS, useValue: MONTH_YEAR_FORMATS }],
  template: `
    <input
      class="month-input"
      readonly
      aria-label="Mese"
      [placeholder]="disabled() ? 'Nessun mese' : 'Scegli il mese'"
      [matDatepicker]="picker"
      [matDatepickerFilter]="isAvailable"
      [value]="selectedDate()"
      [min]="minDate()"
      [max]="maxDate()"
      [disabled]="disabled()"
      (click)="picker.open()"
      (keydown.enter)="picker.open()"
    />
    <mat-datepicker #picker startView="multi-year" [startAt]="selectedDate()" (monthSelected)="choose($event, picker)" />
  `,
  styles: `
    :host { display: flex; flex: 1; min-width: 150px; }
    .month-input {
      width: 100%; border: none; outline: none; background: transparent; cursor: pointer;
      color: var(--app-text); font: inherit; font-size: var(--app-text-md); font-weight: 600;
      text-align: center; text-transform: capitalize;
    }
    .month-input:disabled { cursor: default; color: var(--app-text-muted); }
    .month-input:focus-visible { text-decoration: underline; }
  `,
})
export class MonthPicker {
  readonly value = input<TransactionMonthOption | null>(null);
  /** mesi disponibili, dal più recente */
  readonly months = input.required<TransactionMonthOption[]>();
  readonly disabled = input(false);
  readonly monthChange = output<string>();

  protected readonly selectedDate = computed(() => {
    const value = this.value();
    return value ? parseISO(value.startDate) : null;
  });
  protected readonly minDate = computed(() => {
    const last = this.months().at(-1);
    return last ? parseISO(last.startDate) : null;
  });
  protected readonly maxDate = computed(() => {
    const first = this.months()[0];
    return first ? parseISO(first.endDate) : null;
  });
  private readonly byKey = computed(() => new Map(this.months().map((m) => [m.startDate.slice(0, 7), m])));

  // il calendario abilita un mese (o un anno) se almeno un suo giorno passa il filtro
  protected readonly isAvailable = (date: Date | null) => !!date && this.byKey().has(monthKey(date));

  protected choose(date: Date, picker: MatDatepicker<Date>) {
    picker.close();
    const month = this.byKey().get(monthKey(date));
    if (month) this.monthChange.emit(month.startDate);
  }
}
