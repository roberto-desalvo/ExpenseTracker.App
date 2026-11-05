import { ChangeDetectionStrategy, Component, DestroyRef, inject } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { MatTooltip } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { ROUTES } from "../../core/config/routes";
import { ImportLog, ImportStatus } from "../../core/models/ImportLog";
import { ImportsStore } from "../../core/state/imports.store";
import { DataTable, DataTableColumn } from "../../shared/ui/data-table";
import { PageContainer } from "../../shared/ui/page-container";
import { PageHeader } from "../../shared/ui/page-header";
import { TransactionsTabs } from "../transactions/transactions-tabs";
import { ImportButton } from "./import-button";

const dateTimeFormatter = new Intl.DateTimeFormat("it-IT", { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
const dayFormatter = new Intl.DateTimeFormat("it-IT", { day: "2-digit", month: "short", year: "numeric" });

const STATUS_LABELS: Record<ImportStatus, { label: string; icon: string }> = {
  Queued: { label: "In coda", icon: "schedule" },
  Processing: { label: "In elaborazione", icon: "sync" },
  Completed: { label: "Completato", icon: "check_circle" },
  Failed: { label: "Non riuscito", icon: "error" },
};

const COLUMNS: DataTableColumn[] = [
  { id: "importedOn", label: "Data", minWidth: "15%" },
  { id: "account", label: "Conto", minWidth: "14%" },
  { id: "file", label: "File", minWidth: "27%" },
  { id: "status", label: "Stato", minWidth: "15%" },
  { id: "period", label: "Periodo", minWidth: "15%" },
  { id: "new", label: "Nuovi", minWidth: "7%", align: "right" },
  { id: "skipped", label: "Già presenti", minWidth: "7%", align: "right" },
];

/** Movimenti → Import: storico degli import da file (app, OneDrive, script) e avanzamento di quelli in corso. */
@Component({
  selector: "app-imports-page",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageContainer, PageHeader, TransactionsTabs, ImportButton, DataTable, MatIconModule, MatProgressSpinner, MatTooltip, RouterLink],
  templateUrl: "./imports-page.html",
  styleUrl: "./imports-page.scss",
})
export class ImportsPage {
  protected readonly store = inject(ImportsStore);
  protected readonly columns = COLUMNS;
  protected readonly settingsAccounts = ROUTES.settings;

  constructor() {
    inject(DestroyRef).onDestroy(this.store.activate());
  }

  protected status(log: ImportLog) {
    return STATUS_LABELS[log.status];
  }

  protected dateTime(value: string) {
    return dateTimeFormatter.format(new Date(value));
  }

  protected period(log: ImportLog) {
    if (!log.minTransactionDate || !log.maxTransactionDate) return "—";
    const from = dayFormatter.format(new Date(log.minTransactionDate));
    const to = dayFormatter.format(new Date(log.maxTransactionDate));
    return from === to ? from : `${from} – ${to}`;
  }

  protected isActive(log: ImportLog) {
    return log.status === "Queued" || log.status === "Processing";
  }
}
