import { useMemo, useState } from "react";
import { Autocomplete, TextField } from "@mui/material";
import { DatePicker, LocalizationProvider } from "@mui/x-date-pickers";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import dayjs from "dayjs";
import { useAccounts } from "../stores/AccountContext";
import { useTransactions } from "../stores/TransactionContext";
import AppModal from "./AppModal";
import { TransferPayload } from "../models/Transfer";

interface TransferModalProps {
  open: boolean;
  onClose: () => void;
}

const createDefaultTransfer = (): TransferPayload => ({
  fromAccountId: 0,
  toAccountId: 0,
  amount: 0,
  description: "",
  date: new Date(),
});

export default function TransferModal({ open, onClose }: TransferModalProps) {
  const accountsContext = useAccounts();
  const transactionsContext = useTransactions();
  const [transfer, setTransfer] = useState<TransferPayload>(createDefaultTransfer());

  const selectedFromAccount = useMemo(
    () => accountsContext.accounts.find((account) => account.id === transfer.fromAccountId) ?? null,
    [accountsContext.accounts, transfer.fromAccountId]
  );

  const selectedToAccount = useMemo(
    () => accountsContext.accounts.find((account) => account.id === transfer.toAccountId) ?? null,
    [accountsContext.accounts, transfer.toAccountId]
  );

  const handleClose = () => {
    onClose();
    setTransfer(createDefaultTransfer());
  };

  const handleSubmit = async (): Promise<string> => {
    await transactionsContext.addTransfer({
      ...transfer,
      amount: Number(transfer.amount),
      description: transfer.description.trim(),
    });

    return "Trasferimento creato";
  };

  return (
    <AppModal
      open={open}
      onClose={handleClose}
      title="Nuovo trasferimento"
      onSubmit={handleSubmit}
      submitLabel="Salva"
    >
      <LocalizationProvider dateAdapter={AdapterDayjs}>
        <DatePicker
          label="Data"
          sx={{ width: "100%" }}
          value={transfer.date ? dayjs(transfer.date) : null}
          onChange={(value) => {
            setTransfer((prev) => ({
              ...prev,
              date: value?.toDate() ?? null,
            }));
          }}
        />
      </LocalizationProvider>

      <TextField
        sx={{ width: "100%" }}
        label="Descrizione"
        variant="outlined"
        value={transfer.description}
        onChange={(e) => {
          const { value } = e.target;
          setTransfer((prev) => ({ ...prev, description: value }));
        }}
      />

      <TextField
        label="Importo"
        sx={{ width: "100%" }}
        type="number"
        value={transfer.amount}
        onChange={(e) => {
          const { value } = e.target;
          setTransfer((prev) => ({ ...prev, amount: Number(value) }));
        }}
      />

      <Autocomplete
        disablePortal
        options={accountsContext.accounts}
        getOptionLabel={(account) => account.name || ""}
        isOptionEqualToValue={(option, value) => option.id === value.id}
        value={selectedFromAccount}
        onChange={(_event, account) => {
          setTransfer((prev) => ({
            ...prev,
            fromAccountId: account?.id ?? 0,
          }));
        }}
        sx={{ width: "100%" }}
        renderInput={(params) => <TextField {...params} label="Da conto" />}
      />

      <Autocomplete
        disablePortal
        options={accountsContext.accounts}
        getOptionLabel={(account) => account.name || ""}
        isOptionEqualToValue={(option, value) => option.id === value.id}
        value={selectedToAccount}
        onChange={(_event, account) => {
          setTransfer((prev) => ({
            ...prev,
            toAccountId: account?.id ?? 0,
          }));
        }}
        sx={{ width: "100%" }}
        renderInput={(params) => <TextField {...params} label="A conto" />}
      />
    </AppModal>
  );
}
