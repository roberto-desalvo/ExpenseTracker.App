import "./App.css";
import ExpenseTable from "./components/ExpenseTable";
import HomeHeader from "./components/HomeHeader";
import TransactionModal from "./components/TransactionModal";
import AccountsBar from "./components/AccountsBar";

function App() {
  return (
    <div className="flex flex-col h-screen w-full bg-gray-800 overflow-hidden">
      <HomeHeader/>
      <AccountsBar/>
      <main className="flex-1 min-h-0 px-2 pb-2">
        <ExpenseTable/>
        <TransactionModal/>
      </main>
    </div>
  );
}

export default App;
