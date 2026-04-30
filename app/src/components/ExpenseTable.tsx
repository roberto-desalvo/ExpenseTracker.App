import ExpenseTableRow from "./ExpenseTableRow";
import DataTableBase from "./DataTableBase";
import { useTableContext } from "../stores/TableContext";
import { useTransactions } from "../stores/TransactionContext";

export default function ExpenseTable() {
  const tableContext = useTableContext();
  const transactionContext = useTransactions();
  const filteredTransactions = tableContext.getFilteredTransactions();

  return (
    <DataTableBase      
      columns={tableContext.columns as unknown as import("./DataTableBase").DataTableColumn[]}
      rows={filteredTransactions}
      isLoading={transactionContext.isLoading}
      isEmpty={!transactionContext.isLoading && filteredTransactions.length === 0}
      emptyMessage="Nessuna transazione trovata"
      emptySubtext="Modifica i filtri o aggiungi una nuova transazione"
      page={tableContext.page}
      pageSize={tableContext.pageSize}
      totalCount={transactionContext.totalCount}
      onPageChange={(_event, newPage) => tableContext.modifyPage(newPage)}
      onPageSizeChange={(event) => tableContext.modifyPageSize(+event.target.value)}
      rowsPerPageOptions={[10, 25, 100]}
      renderRow={(t) => <ExpenseTableRow transaction={t} key={t.id} />}    />
  );
}
