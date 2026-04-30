import AccountsBar from "../components/AccountsBar";
import ExpenseTable from "../components/ExpenseTable";
import TransactionModal from "../components/TransactionModal";

export default function TransactionsPage() {
  return (
    <>
      <AccountsBar />
      <main className="flex-1 min-h-0 px-2 pb-2">
        <ExpenseTable />
        <TransactionModal />
      </main>
    </>
  );
}
