import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, signal } from "@angular/core";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatTooltip } from "@angular/material/tooltip";
import { NotificationService } from "../../core/api/notification.service";
import { TransactionApi } from "../../core/api/transaction.api";
import Account from "../../core/models/Account";
import { LandingAccountBalance } from "../../core/models/LandingDashboard";
import { AccountsStore } from "../../core/state/accounts.store";
import { TransactionsStore } from "../../core/state/transactions.store";
import { ThemeService } from "../../core/theme/theme.service";
import { formatCurrency, formatSignedCurrency, initialsOf } from "../../core/utils/format.utilities";
import { buildEntityColorMap } from "../../shared/charts/entityColors";
import { EditorDrawer } from "../../shared/ui/editor-drawer";
import { EmptyState } from "../../shared/ui/empty-state";
import { Pagination } from "../../shared/ui/pagination";
import { SearchField } from "../../shared/ui/search-field";

/** Impostazioni → Conti: card con saldo attuale e netto del mese, creazione e modifica nel pannello laterale. */
@Component({
  selector: "app-accounts-settings",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButton,
    MatIconButton,
    MatIconModule,
    MatTooltip,
    MatFormFieldModule,
    MatInputModule,
    EditorDrawer,
    EmptyState,
    Pagination,
    SearchField,
  ],
  templateUrl: "./accounts-settings.html",
  styleUrl: "./accounts-settings.scss",
})
export class AccountsSettings {
  protected readonly store = inject(AccountsStore);
  private readonly transactions = inject(TransactionsStore);
  private readonly transactionApi = inject(TransactionApi);
  private readonly notifications = inject(NotificationService);
  private readonly theme = inject(ThemeService);

  protected readonly balances = signal<Map<number, LandingAccountBalance>>(new Map());
  protected readonly balancesLoading = signal(true);
  protected readonly editorOpen = signal(false);
  protected readonly editing = signal<Account | null>(null);
  protected readonly name = signal("");
  protected readonly iban = signal("");
  protected readonly busy = signal(false);

  protected readonly formatCurrency = formatCurrency;
  protected readonly formatSignedCurrency = formatSignedCurrency;
  protected readonly initialsOf = initialsOf;

  protected readonly pageCount = computed(() => Math.max(Math.ceil(this.store.totalCount() / this.store.pageSize()), 1));

  // stessi colori dei grafici: l'identità del conto resta coerente in tutta l'app
  private readonly colorMap = computed(() => {
    const chart = this.theme.tokens().chart;
    return buildEntityColorMap(
      this.store.all().map((x) => x.id),
      chart.series,
      chart.other,
    );
  });

  // chiave stabile: cambia solo se cambia l'insieme dei conti (es. conto aggiunto), non il riferimento all'array
  private readonly accountIdsKey = computed(() =>
    this.store
      .all()
      .map((x) => x.id)
      .join(","),
  );

  constructor() {
    // la lista paginata si carica solo mentre questa sezione è visibile
    inject(DestroyRef).onDestroy(this.store.activatePagedList());

    effect(() => {
      this.transactions.dataVersion();
      this.accountIdsKey();
      if (!this.store.loaded()) return;
      void this.loadBalances();
    });
  }

  protected colorOf(account: Account) {
    return this.colorMap().get(account.id) ?? this.theme.tokens().chart.other;
  }

  private async loadBalances() {
    this.balancesLoading.set(true);
    try {
      const landing = await this.transactionApi.getLanding({ excludeTransfers: true });
      this.balances.set(new Map(landing.accounts.map((x) => [x.accountId, x])));
    } catch {
      this.balances.set(new Map());
    } finally {
      this.balancesLoading.set(false);
    }
  }

  protected inputValue(event: Event) {
    return (event.target as HTMLInputElement).value;
  }

  protected openCreate() {
    this.editing.set(null);
    this.name.set("");
    this.iban.set("");
    this.editorOpen.set(true);
  }

  protected openEdit(account: Account) {
    this.editing.set(account);
    this.name.set(account.name);
    this.iban.set(account.iban ?? "");
    this.editorOpen.set(true);
  }

  protected async submit() {
    const trimmed = this.name().trim();
    if (!trimmed) return;
    // il BE normalizza (maiuscolo, senza spazi) e valida l'IBAN
    const normalizedIban = this.iban().replace(/\s+/g, "").toUpperCase() || null;
    const editing = this.editing();
    this.busy.set(true);
    try {
      if (editing) {
        await this.store.updateAccount({ id: editing.id, name: trimmed, iban: normalizedIban });
        this.notifications.showSuccess(`Conto "${trimmed}" aggiornato`);
      } else {
        await this.store.addAccount({ id: 0, name: trimmed, iban: normalizedIban });
        this.notifications.showSuccess(`Conto "${trimmed}" creato`);
      }
      this.editorOpen.set(false);
    } catch {
      // errore mostrato dal layer API
    } finally {
      this.busy.set(false);
    }
  }
}
