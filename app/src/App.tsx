import "./App.css";
import ExpenseTable from "./components/ExpenseTable";
import HomeHeader from "./components/HomeHeader";
import TransactionModal from "./components/TransactionModal";
import TableAside from "./components/TableAside";
import AccountsBar from "./components/AccountsBar";

function App() {
  return (
    <div className="flex flex-col h-screen w-screen bg-gray-800">
      <HomeHeader/>
      <AccountsBar/>
      <div className="flex flex-1 px-2">
        <main className="flex-1 ">
          <ExpenseTable/>
          <TransactionModal/>
        </main>
        <TableAside/>
      </div>
    </div>
  );
}

export default App;
