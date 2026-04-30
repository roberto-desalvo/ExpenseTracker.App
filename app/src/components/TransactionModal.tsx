import {
  Autocomplete,
  TextField,
} from "@mui/material";
import { useTransactionModal } from "../stores/TransactionModalContext";
import { useCategories } from "../stores/CategoryContext";
import { DatePicker, LocalizationProvider } from "@mui/x-date-pickers";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { useAccounts } from "../stores/AccountContext";
import dayjs from "dayjs";
import AppModal from "./AppModal";

export default function TransactionModal() {
  const transactionModalContext = useTransactionModal();
  const categoryContext = useCategories();
  const accountContext = useAccounts();

  const isEdit =
    transactionModalContext.currentTransaction != null &&
    transactionModalContext.currentTransaction.id > 0;

  const handleSubmit = async (e: React.FormEvent): Promise<string> => {
    e.preventDefault();
    await transactionModalContext.sendTransaction();
    return isEdit ? "Transazione modificata" : "Transazione creata";
  };

  return (
    <AppModal
      open={transactionModalContext.transactionModalOpen}
      onClose={() => transactionModalContext.closeTransactionModal()}
      title={isEdit ? "Modifica transazione" : "Nuova transazione"}
      onSubmit={handleSubmit}
      submitLabel="Salva"
    >
      <LocalizationProvider dateAdapter={AdapterDayjs}>
        <DatePicker
          label="Data"
          sx={{ width: "100%" }}
          onChange={(e) => transactionModalContext.modifyDate(e?.toDate())}
          value={
            transactionModalContext.currentTransaction?.date
              ? dayjs(transactionModalContext.currentTransaction.date)
              : null
          }
        />
      </LocalizationProvider>
      <TextField
        sx={{ width: "100%" }}
        label="Descrizione"
        variant="outlined"
        value={transactionModalContext.currentTransaction?.description ?? ""}
        onChange={(e) => transactionModalContext.modifyDescription(e.target.value)}
      />
      <TextField
        label="Importo"
        sx={{ width: "100%" }}
        type="number"
        value={transactionModalContext.currentTransaction?.amount ?? ""}
        onChange={(e) => transactionModalContext.modifyAmount(Number(e.target.value))}
      />
      <Autocomplete
        disablePortal
        options={categoryContext.categories}
        getOptionLabel={(category) => category.description || ""}
        isOptionEqualToValue={(option, value) => option.id === value.id}
        value={categoryContext.categories.find(
          (c) => c.id === transactionModalContext.currentTransaction?.categoryId,
        )}
        onChange={(_event, category) => transactionModalContext.modifyCategory(category)}
        sx={{ width: "100%" }}
        renderInput={(params) => <TextField {...params} label="Categoria" />}
      />
      <Autocomplete
        disablePortal
        options={accountContext.accounts}
        getOptionLabel={(account) => account.name || ""}
        isOptionEqualToValue={(option, value) => option.id === value.id}
        value={accountContext.accounts.find(
          (a) => a.id === transactionModalContext.currentTransaction?.accountId,
        )}
        onChange={(_event, account) => transactionModalContext.modifyAccount(account)}
        sx={{ width: "100%" }}
        renderInput={(params) => <TextField {...params} label="Conto" />}
      />
    </AppModal>
  );
}
