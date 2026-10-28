import PageContainer from "../components/ui/PageContainer";
import PageHeader from "../components/ui/PageHeader";
import TransactionFilters from "../components/TransactionFilters";
import ExpenseTable from "../components/ExpenseTable";
import { NewEntryButton } from "../components/layout/HeaderActions";

export default function TransactionsPage() {
  return (
    <PageContainer>
      <PageHeader
        title="Movimenti"
        subtitle="Le transazioni del periodo selezionato. Passa sopra una riga per modificarla o eliminarla."
        actions={<NewEntryButton />}
      />
      <TransactionFilters />
      <ExpenseTable />
    </PageContainer>
  );
}
