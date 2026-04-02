import { IconButton, Stack } from "@mui/material";
import RefreshIcon from "@mui/icons-material/Refresh";
import AddIcon from "@mui/icons-material/Add";
import { useTransactions } from "../stores/TransactionContext";
import { useTransactionModal } from "../stores/TransactionModalContext";

export default function HomeHeader() {
  const transactionsContext = useTransactions();
  const transactionModalContext = useTransactionModal();

  return (
    <>
      <header className="h-1/8 text-white flex items-center py-4 bg-gray-900 shadow-xl">
        <Stack
          direction="row"
          spacing={2}
          alignItems="center"
          sx={{ padding: "0 0.5rem" }}
        >
          <IconButton
            sx={{ border: "1px solid #cddc39" }}
            onClick={() => transactionsContext.refreshTransactions()}
          >
            <RefreshIcon className="text-lime-500 hover:cursor-pointer hover:#424242 hover:rounded-full hover:shadow-[0_0_15px_5px_rgba(192,233,89,0.4)] transition-all duration-300"/>
          </IconButton>
          <IconButton
            sx={{ border: "1px solid #cddc39" }}
            onClick={() => transactionModalContext.openTransactionModal()}
          >
            <AddIcon className="text-lime-500 hover:cursor-pointer hover:#424242 hover:rounded-full hover:shadow-[0_0_15px_5px_rgba(192,233,89,0.4)] transition-all duration-300"/>
          </IconButton>
          <h1 className="text-lime-500 text-2xl font-bold px-6">
            Expense Tracker
          </h1>
        </Stack>
      </header>
    </>
  );
}
