import { Location } from "@angular/common";
import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, signal, untracked } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { ROUTES } from "../../core/config/routes";
import { DRILL_DOWN_PARAMS, parseDrillDown } from "../../core/navigation/drill-down";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatDividerModule } from "@angular/material/divider";
import { MatIconModule } from "@angular/material/icon";
import { MatMenuModule } from "@angular/material/menu";
import { MatTooltip } from "@angular/material/tooltip";
import Transaction from "../../core/models/Transaction";
import { TransactionSortField } from "../../core/models/TransactionQueryRequest";
import { CurrentUserStore } from "../../core/state/current-user.store";
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
import { BulkCategoryModal } from "./bulk-category-modal";
import { TransactionFilters } from "./transaction-filters";
import { SplitTransactionModal } from "./split-transaction-modal";
import { TransferCounterpartModal } from "./transfer-counterpart-modal";
import { TransactionsTabs } from "./transactions-tabs";

const dayFormatter = new Intl.DateTimeFormat("it-IT", { day: "2-digit", month: "short", year: "numeric" });
const weekdayFormatter = new Intl.DateTimeFormat("it-IT", { weekday: "long" });

/**
 * Movimenti: filtri del periodo e tabella paginata/ordinata lato server, con menu per riga (modifica, duplica, scomponi, elimina)
 * e selezione multipla (righe della pagina corrente) per eliminare o assegnare una categoria in blocco.
 * Con il filtro Trasferimenti le righe con un trasferimento incompleto, o in Money transfers senza trasferimento,
 * sono segnalate e si può collegarne il corrispondente.
 */
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
    BulkCategoryModal,
    TransferCounterpartModal,
    SplitTransactionModal,
    MatButton,
    MatIconButton,
    MatIconModule,
    MatMenuModule,
    MatDividerModule,
    MatTooltip,
    RouterLink,
  ],
  templateUrl: "./transactions-page.html",
  styleUrl: "./transactions-page.scss",
})
export class TransactionsPage {
  protected readonly table = inject(TransactionsTableStore);
  protected readonly store = inject(TransactionsStore);
  protected readonly quickActions = inject(QuickActionsService);
  protected readonly currentUser = inject(CurrentUserStore);
  protected readonly routes = ROUTES;

  protected readonly toDelete = signal<Transaction | null>(null);
  /** Movimento di cui collegare il ramo mancante del trasferimento. */
  protected readonly toLink = signal<Transaction | null>(null);
  /** Movimento da scomporre in più voci. */
  protected readonly toSplit = signal<Transaction | null>(null);

  /** Motivo del warning sul trasferimento di una riga, oppure null se non c'è nulla da collegare. */
  protected transferWarning(t: Transaction): string | null {
    if (t.transferMissing) return "Categoria Money transfers senza trasferimento collegato: collega il movimento corrispondente";
    if (t.transferIncomplete) return "Trasferimento senza il ramo opposto: collega il movimento corrispondente";
    return null;
  }
  protected readonly formatSignedCurrency = formatSignedCurrency;
  protected readonly deleteMessage = computed(() => {
    const t = this.toDelete();
    if (!t) return "";
    return `Vuoi eliminare “${t.description || "questa transazione"}” (${formatSignedCurrency(t.amount)})? L'operazione non può essere annullata.`;
  });

  protected readonly rowKey = (t: Transaction) => t.id;
  protected readonly selectedIds = signal<ReadonlySet<number>>(new Set());
  protected readonly selectedCount = computed(() => this.selectedIds().size);
  protected readonly selectedList = computed(() => [...this.selectedIds()]);
  protected readonly bulkDeleteOpen = signal(false);
  protected readonly bulkCategoryOpen = signal(false);
  protected readonly bulkDeleteMessage = computed(() => {
    const count = this.selectedCount();
    const what = count === 1 ? "il movimento selezionato" : `i ${count} movimenti selezionati`;
    return `Vuoi eliminare ${what}? L'operazione non può essere annullata.`;
  });

  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly query = toSignal(inject(ActivatedRoute).queryParamMap);
  // pagina raggiunta da un'altra pagina dell'app (non aperta direttamente da URL)
  private readonly hasInAppHistory = this.router.lastSuccessfulNavigation() !== null;

  protected readonly fromAnalytics = computed(() => this.query()?.get(DRILL_DOWN_PARAMS.source) === "analisi");

  constructor() {
    // mesi disponibili e lista vengono caricati solo mentre la pagina è aperta
    const deactivateStore = this.store.activatePage();
    const deactivateTable = this.table.activate();
    inject(DestroyRef).onDestroy(() => {
      deactivateTable();
      deactivateStore();
    });

    // la selezione riguarda solo le righe visibili: a ogni nuova lista restano selezionate quelle ancora presenti
    effect(() => {
      const visible = new Set(this.store.transactions().map((t) => t.id));
      untracked(() =>
        this.selectedIds.update((prev) => {
          const next = new Set([...prev].filter((id) => visible.has(id)));
          return next.size === prev.size ? prev : next;
        }),
      );
    });

    // drill-down (es. da Analisi): i filtri arrivano dai query param e restano nell'URL, così la cronologia li ripristina
    effect(() => {
      const query = this.query();
      const filter = query && parseDrillDown(query);
      if (filter) untracked(() => this.table.applyDrillDown(filter));
    });
  }

  protected backToAnalytics() {
    if (this.hasInAppHistory) this.location.back();
    else void this.router.navigateByUrl(ROUTES.analytics);
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

  protected clearSelection() {
    this.selectedIds.set(new Set());
  }

  protected readonly confirmBulkDelete = async (): Promise<string> => {
    const ids = this.selectedList();
    await this.store.deleteTransactions(ids);
    this.clearSelection();
    return ids.length === 1 ? "1 movimento eliminato" : `${ids.length} movimenti eliminati`;
  };

  protected readonly confirmDelete = (): string => {
    const transaction = this.toDelete();
    if (transaction) void this.store.deleteTransaction(transaction);
    return "Transazione eliminata";
  };
}
