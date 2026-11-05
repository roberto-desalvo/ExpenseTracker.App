import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from "@angular/core";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltip } from "@angular/material/tooltip";
import Transaction from "../../core/models/Transaction";
import { TransactionSortField } from "../../core/models/TransactionQueryRequest";
import { QuickActionsService } from "../../core/state/quick-actions.service";
import { TransactionsTableStore } from "../../core/state/transactions-table.store";
import { TransactionsStore } from "../../core/state/transactions.store";
import { formatSignedCurrency } from "../../core/utils/format.utilities";
import { NewEntryButton } from "../../layout/header-actions";
import { ImportButton } from "../imports/import-button";
import { ConfirmDeleteDialog } from "../../shared/ui/confirm-delete-dialog";
import { DataTable } from "../../shared/ui/data-table";
import { PageContainer } from "../../shared/ui/page-container";
import { PageHeader } from "../../shared/ui/page-header";
import { TransactionFilters } from "./transaction-filters";
import { TransactionsTabs } from "./transactions-tabs";

const dayFormatter = new Intl.DateTimeFormat("it-IT", { day: "2-digit", month: "short", year: "numeric" });
const weekdayFormatter = new Intl.DateTimeFormat("it-IT", { weekday: "long" });

/** Movimenti: filtri del periodo e tabella paginata/ordinata lato server, con modifica ed eliminazione per riga. */
@Component({
  selector: "app-transactions-page",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainer,
    PageHeader,
    NewEntryButton,
    ImportButton,
    TransactionsTabs,
    TransactionFilters,
    DataTable,
    ConfirmDeleteDialog,
    MatButton,
    MatIconButton,
    MatIconModule,
    MatTooltip,
  ],
  templateUrl: "./transactions-page.html",
  styleUrl: "./transactions-page.scss",
})
export class TransactionsPage {
  protected readonly table = inject(TransactionsTableStore);
  protected readonly store = inject(TransactionsStore);
  protected readonly quickActions = inject(QuickActionsService);

  protected readonly toDelete = signal<Transaction | null>(null);
  protected readonly formatSignedCurrency = formatSignedCurrency;
  protected readonly deleteMessage = computed(() => {
    const t = this.toDelete();
    if (!t) return "";
    return `Vuoi eliminare “${t.description || "questa transazione"}” (${formatSignedCurrency(t.amount)})? L'operazione non può essere annullata.`;
  });

  constructor() {
    // mesi disponibili e lista vengono caricati solo mentre la pagina è aperta
    const deactivateStore = this.store.activatePage();
    const deactivateTable = this.table.activate();
    inject(DestroyRef).onDestroy(() => {
      deactivateTable();
      deactivateStore();
    });
  }

  protected day(date: Date | string) {
    return dayFormatter.format(new Date(date));
  }

  protected weekday(date: Date | string) {
    return weekdayFormatter.format(new Date(date));
  }

  protected sort(columnId: string) {
    this.table.modifySort(columnId as TransactionSortField);
  }

  protected readonly confirmDelete = (): string => {
    const transaction = this.toDelete();
    if (transaction) void this.store.deleteTransaction(transaction);
    return "Transazione eliminata";
  };
}
