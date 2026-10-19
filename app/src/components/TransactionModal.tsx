import { useEffect, useState } from "react";
import { Autocomplete, InputAdornment, TextField, ToggleButton, ToggleButtonGroup } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { NorthEastRounded, SouthWestRounded } from "@mui/icons-material";
import { DatePicker, LocalizationProvider } from "@mui/x-date-pickers";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import dayjs from "dayjs";
import "dayjs/locale/it";
import { useTransactionModal } from "../stores/TransactionModalContext";
import { useCategories } from "../stores/CategoryContext";
import { useAccounts } from "../stores/AccountContext";
import AppModal from "./AppModal";

type Kind = "expense" | "income";

export default function TransactionModal() {
  const a = useTheme().app;
  const transactionModalContext = useTransactionModal();
  const { allCategories } = useCategories();
  const accountContext = useAccounts();
  const current = transactionModalContext.currentTransaction;
  const open = transactionModalContext.transactionModalOpen;

  const [kind, setKind] = useState<Kind>("expense");
  const [amountText, setAmountText] = useState("");

  // all'apertura il tipo deriva dal segno dell'importo esistente
  useEffect(() => {
    if (open) {
      setKind((current?.amount ?? 0) > 0 ? "income" : "expense");
      setAmountText(current?.amount ? String(Math.abs(current.amount)) : "");
    }
  }, [open, current?.id]);

  const isEdit = current != null && current.id > 0;

  const applyAmount = (value: number, nextKind: Kind) => {
    transactionModalContext.modifyAmount(nextKind === "expense" ? -Math.abs(value) : Math.abs(value));
  };

  const handleSubmit = async (e: React.FormEvent): Promise<string> => {
    e.preventDefault();
    await transactionModalContext.sendTransaction();
    return isEdit ? "Transazione modificata" : "Transazione creata";
  };

  return (
    <AppModal
      open={open}
      onClose={() => transactionModalContext.closeTransactionModal()}
      title={isEdit ? "Modifica transazione" : "Nuova transazione"}
      subtitle={isEdit ? undefined : "Registra un'entrata o un'uscita su uno dei tuoi conti"}
      onSubmit={handleSubmit}
      submitLabel={isEdit ? "Salva modifiche" : "Aggiungi"}
    >
      <ToggleButtonGroup
        exclusive
        fullWidth
        value={kind}
        onChange={(_e, value: Kind | null) => {
          if (!value) return;
          setKind(value);
          if (current?.amount) applyAmount(current.amount, value);
        }}
        aria-label="Tipo di movimento"
      >
        <ToggleButton value="expense" sx={{ gap: 0.75, "&.Mui-selected": { color: a.expense } }}>
          <NorthEastRounded sx={{ fontSize: 17 }} /> Uscita
        </ToggleButton>
        <ToggleButton value="income" sx={{ gap: 0.75, "&.Mui-selected": { color: a.income } }}>
          <SouthWestRounded sx={{ fontSize: 17 }} /> Entrata
        </ToggleButton>
      </ToggleButtonGroup>

      <TextField
        label="Importo"
        type="number"
        required
        autoFocus={!isEdit}
        value={amountText}
        onChange={(e) => {
          setAmountText(e.target.value);
          applyAmount(Number(e.target.value) || 0, kind);
        }}
        slotProps={{
          input: { startAdornment: <InputAdornment position="start">€</InputAdornment> },
          htmlInput: { min: 0, step: "0.01", inputMode: "decimal" },
        }}
      />

      <TextField
        label="Descrizione"
        placeholder="Es. Spesa supermercato"
        value={current?.description ?? ""}
        onChange={(e) => transactionModalContext.modifyDescription(e.target.value)}
      />

      <LocalizationProvider dateAdapter={AdapterDayjs} adapterLocale="it">
        <DatePicker
          label="Data"
          sx={{ width: "100%" }}
          onChange={(value) => transactionModalContext.modifyDate(value?.toDate())}
          value={current?.date ? dayjs(current.date) : null}
        />
      </LocalizationProvider>

      <Autocomplete
        disablePortal
        options={allCategories}
        getOptionLabel={(category) => category.name || category.description || ""}
        isOptionEqualToValue={(option, value) => option.id === value.id}
        value={allCategories.find((c) => c.id === current?.categoryId) ?? null}
        onChange={(_event, category) => transactionModalContext.modifyCategory(category)}
        renderInput={(params) => <TextField {...params} label="Categoria" />}
      />
      <Autocomplete
        disablePortal
        options={accountContext.accounts}
        getOptionLabel={(account) => account.name || ""}
        isOptionEqualToValue={(option, value) => option.id === value.id}
        value={accountContext.accounts.find((x) => x.id === current?.accountId) ?? null}
        onChange={(_event, account) => transactionModalContext.modifyAccount(account)}
        renderInput={(params) => <TextField {...params} label="Conto" required />}
      />
    </AppModal>
  );
}
