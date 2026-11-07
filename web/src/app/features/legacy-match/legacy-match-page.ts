import { ChangeDetectionStrategy, Component, computed, inject, signal } from "@angular/core";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatCheckbox } from "@angular/material/checkbox";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInput } from "@angular/material/input";
import { MatProgressBar } from "@angular/material/progress-bar";
import { MatSelectModule } from "@angular/material/select";
import { MatSlideToggle } from "@angular/material/slide-toggle";
import { MatTooltip } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { ROUTES } from "../../core/config/routes";
import { LegacyDbTransaction, LegacySheet } from "../../core/models/LegacyMatch";
import { CategoriesStore } from "../../core/state/categories.store";
import { formatPeriodLabel, formatSignedCurrency } from "../../core/utils/format.utilities";
import { ConfirmDeleteDialog } from "../../shared/ui/confirm-delete-dialog";
import { EmptyState } from "../../shared/ui/empty-state";
import { PageContainer } from "../../shared/ui/page-container";
import { PageHeader } from "../../shared/ui/page-header";
import { SurfaceCard } from "../../shared/ui/surface-card";
import { LegacyCreateDialog } from "./legacy-create-dialog";
import { LegacyMatchStore } from "./legacy-match.store";

const dayFormatter = new Intl.DateTimeFormat("it-IT", { day: "2-digit", month: "short" });

interface AccountOption {
  id: number;
  name: string;
}

/**
 * Movimenti → Abbinamento storico Excel (solo admin): a sinistra le righe di un mese del vecchio
 * Spese.xlsx, a destra i movimenti a DB dello stesso mese (più 7 giorni
 * prima e dopo). Le coppie suggerite (stesso conto,
 * importo ±0,02 €) si possono correggere; al salvataggio il movimento prende la categoria Excel e
 * la descrizione Excel (o, con l'icona apposita, "DB - Excel"), entrambe modificabili.
 */
@Component({
  selector: "app-legacy-match-page",
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [LegacyMatchStore],
  imports: [
    PageContainer,
    PageHeader,
    SurfaceCard,
    EmptyState,
    RouterLink,
    MatButton,
    MatIconButton,
    MatIconModule,
    MatTooltip,
    MatFormFieldModule,
    MatSelectModule,
    MatInput,
    MatCheckbox,
    MatSlideToggle,
    MatProgressBar,
    LegacyCreateDialog,
    ConfirmDeleteDialog,
  ],
  templateUrl: "./legacy-match-page.html",
  styleUrl: "./legacy-match-page.scss",
})
export class LegacyMatchPage {
  protected readonly store = inject(LegacyMatchStore);
  protected readonly categories = inject(CategoriesStore);
  protected readonly routes = ROUTES;
  protected readonly formatSignedCurrency = formatSignedCurrency;

  protected readonly accountFilter = signal<number | null>(null);
  protected readonly showMatched = signal(false);

  protected readonly accountOptions = computed<AccountOption[]>(() => {
    const byId = new Map<number, string>();
    for (const t of this.store.dbTransactions()) byId.set(t.accountId, t.account);
    return [...byId].map(([id, name]) => ({ id, name })).sort((a, b) => a.name.localeCompare(b.name));
  });

  protected readonly visibleRows = computed(() => {
    const account = this.accountFilter();
    // di default solo le righe ancora da gestire, così restano in vista quelle che mancano
    const rows = this.showMatched() ? this.store.matchRows() : this.store.pendingRows();
    return rows.filter((r) => account === null || r.accountId === account);
  });

  protected readonly visibleTransactions = computed(() => {
    const account = this.accountFilter();
    const paired = this.store.rowByTransaction();
    return this.store
      .dbTransactions()
      .filter((t) => account === null || t.accountId === account)
      .filter((t) => this.showMatched() || (!t.legacyMatchedAt && !paired.has(t.id)));
  });

  /** Movimento abbinato alla riga selezionata, evidenziato a destra. */
  protected readonly selectedTransactionId = computed(() => {
    const row = this.store.selectedRow();
    return row === null ? null : (this.store.pairs().get(row)?.transactionId ?? null);
  });

  protected readonly unresolvedAccounts = computed(
    () => this.store.matchRows().filter((r) => r.accountId === null).length,
  );

  /** Stato di una riga gestita, mostrato quando si vedono anche le abbinate. */
  protected rowStatus(rowNumber: number): "saved" | "dismissed" | null {
    if (this.store.savedRows().has(rowNumber)) return "saved";
    if (this.store.dismissedRows().has(rowNumber)) return "dismissed";
    return null;
  }

  protected readonly confirmReset = () => this.store.resetSheet();

  protected readonly resetMessage = computed(() => {
    const sheet = this.store.month()?.sheet;
    const label = sheet ? this.sheetLabel(sheet) : "del mese";
    return `Per ${label} i movimenti creati dallo storico (Contanti, Hype) vengono eliminati; agli altri movimenti abbinati si tolgono l'abbinamento, la descrizione modificata e la categoria Excel, ripristinando la categoria di default.`;
  });

  protected sheetLabel(sheet: LegacySheet) {
    return formatPeriodLabel(`${sheet.year}-${String(sheet.month).padStart(2, "0")}`, true);
  }

  protected existingMatchLabel(t: LegacyDbTransaction) {
    return `Risulta già abbinata: «${t.description}» del ${this.day(t.date)} su ${t.account}, ${formatSignedCurrency(t.amount)}`;
  }

  protected day(date: string | null) {
    return date ? dayFormatter.format(new Date(date)) : "—";
  }

  protected categoryName(id: number | null) {
    return id === null ? null : (this.categories.all().find((c) => c.id === id)?.name ?? null);
  }

  protected onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = "";
    if (file) void this.store.openFile(file);
  }

  protected onDescriptionInput(rowNumber: number, event: Event) {
    this.store.updatePair(rowNumber, { description: (event.target as HTMLInputElement).value });
  }
}
