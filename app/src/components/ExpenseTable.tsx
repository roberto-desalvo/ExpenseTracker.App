import { Button } from "@mui/material";
import { AddRounded } from "@mui/icons-material";
import ExpenseTableRow from "./ExpenseTableRow";
import DataTableBase, { DataTableColumn } from "./DataTableBase";
import { useTableContext } from "../stores/TableContext";
import { useTransactions } from "../stores/TransactionContext";
import { useQuickActions } from "../stores/QuickActionsContext";
import { TransactionSortField } from "../models/TransactionQueryRequest";

export default function ExpenseTable() {
  const tableContext = useTableContext();
  const transactionContext = useTransactions();
  const { openNewTransaction } = useQuickActions();
  // ordinamento e paginazione sono applicati lato server
  const transactions = transactionContext.transactions;

  return (
    <DataTableBase
      columns={tableContext.columns as unknown as DataTableColumn[]}
      rows={transactions}
      isLoading={transactionContext.isLoading}
      isEmpty={!transactionContext.isLoading && transactions.length === 0}
      emptyMessage="Nessun movimento trovato"
      emptySubtext="Prova a cambiare periodo o filtri, oppure registra un nuovo movimento."
      emptyAction={
        <Button variant="contained" startIcon={<AddRounded />} onClick={openNewTransaction}>
          Nuova transazione
        </Button>
      }
      page={tableContext.page}
      pageSize={tableContext.pageSize}
      totalCount={transactionContext.totalCount}
      onPageChange={(_event, newPage) => tableContext.modifyPage(newPage)}
      onPageSizeChange={(event) => tableContext.modifyPageSize(+event.target.value)}
      sortBy={tableContext.sortBy}
      sortDirection={tableContext.sortDirection}
      onSortChange={(columnId) => tableContext.modifySort(columnId as TransactionSortField)}
      rowsPerPageOptions={[10, 20, 50]}
      minTableWidth={760}
      renderRow={(t) => <ExpenseTableRow transaction={t} key={t.id} />}
    />
  );
}
