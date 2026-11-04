import { ChangeDetectionStrategy, Component, computed, inject } from "@angular/core";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatButtonToggleModule } from "@angular/material/button-toggle";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatIconModule } from "@angular/material/icon";
import { MatSelectModule } from "@angular/material/select";
import { MatTooltip } from "@angular/material/tooltip";
import { format, isValid, parseISO } from "date-fns";
import { AccountsStore } from "../../core/state/accounts.store";
import { CategoriesStore } from "../../core/state/categories.store";
import { MovementType, PeriodMode, TransactionsTableStore } from "../../core/state/transactions-table.store";
import { TransactionsStore } from "../../core/state/transactions.store";
import { MultiSelectFilter } from "./multi-select-filter";

const toDate = (value: string | null) => (value ? parseISO(value) : null);
const toKey = (value: Date | null) => (value && isValid(value) ? format(value, "yyyy-MM-dd") : null);

/**
 * Barra filtri dei movimenti: periodo (mese intero con frecce oppure intervallo di date personalizzato),
 * tipo di movimento, conti e categorie.
 */
@Component({
  selector: "app-transaction-filters",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButton,
    MatIconButton,
    MatButtonToggleModule,
    MatDatepickerModule,
    MatIconModule,
    MatSelectModule,
    MatTooltip,
    MultiSelectFilter,
  ],
  templateUrl: "./transaction-filters.html",
  styleUrl: "./transaction-filters.scss",
})
export class TransactionFilters {
  protected readonly table = inject(TransactionsTableStore);
  protected readonly transactions = inject(TransactionsStore);
  private readonly accounts = inject(AccountsStore);
  private readonly categories = inject(CategoriesStore);

  protected readonly accountOptions = computed(() => this.accounts.all().map((a) => ({ id: a.id, name: a.name })));
  protected readonly categoryOptions = computed(() => this.categories.all().map((c) => ({ id: c.id, name: c.name })));

  protected readonly months = this.table.availableMonths;
  protected readonly monthIndex = computed(() =>
    this.months().findIndex((m) => m.startDate === this.table.selectedMonth()?.startDate),
  );

  protected readonly fromValue = computed(() => toDate(this.table.customRange().from));
  protected readonly toValue = computed(() => toDate(this.table.customRange().to));

  protected readonly anyFilter = computed(() => {
    const accountIds = this.table.selectedAccountIds();
    const categoryIds = this.table.selectedCategoryIds();
    const accountFilterOn = accountIds.length > 0 && accountIds.length < this.accounts.all().length;
    const categoryFilterOn = categoryIds.length > 0 && categoryIds.length < this.categories.all().length;
    return accountFilterOn || categoryFilterOn || this.table.movementType() !== "all";
  });

  // i mesi arrivano dal più recente: "precedente" = indice successivo
  protected goTo(index: number) {
    const month = this.months()[index];
    if (month) this.table.modifySelectedMonth(month);
  }

  protected selectMonth(startDate: string) {
    this.goTo(this.months().findIndex((m) => m.startDate === startDate));
  }

  protected changePeriodMode(mode: PeriodMode | null) {
    if (mode) this.table.modifyPeriodMode(mode);
  }

  protected changeMovementType(type: MovementType | null) {
    if (type) this.table.modifyMovementType(type);
  }

  protected changeFrom(value: Date | null) {
    this.table.modifyCustomRange({ from: toKey(value), to: this.table.customRange().to });
  }

  protected changeTo(value: Date | null) {
    this.table.modifyCustomRange({ from: this.table.customRange().from, to: toKey(value) });
  }

  protected resetFilters() {
    this.table.modifySelectedAccountIds([]);
    this.table.modifySelectedCategoryIds([]);
    this.table.modifyMovementType("all");
  }
}
