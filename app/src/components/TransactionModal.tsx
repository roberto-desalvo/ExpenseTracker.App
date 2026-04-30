import {
  Autocomplete,
  Box,
  Button,
  Modal,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useTransactionModal } from "../stores/TransactionModalContext";
import { useCategories } from "../stores/CategoryContext";
import { DatePicker, LocalizationProvider } from "@mui/x-date-pickers";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { useAccounts } from "../stores/AccountContext";
import dayjs, { Dayjs } from "dayjs";
import { useTransactions } from "../stores/TransactionContext";

export default function TransactionModal() {
  const transactionModalContext = useTransactionModal();
  const categoryContext = useCategories();
  const accountContext = useAccounts();
  const transactionContext = useTransactions();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    transactionModalContext.sendTransaction();
    transactionModalContext.closeTransactionModal();
    transactionContext.refreshTransactions();
  };

  return (
    <>
      <Modal
        open={transactionModalContext.transactionModalOpen}
        onClose={() => transactionModalContext.closeTransactionModal()}
        aria-labelledby="modal-modal-title"
        aria-describedby="modal-modal-description"
      >
        <Box
          sx={{
            position: "absolute",
            top: "50%",
            left: "50%",
            transform: "translate(-50%, -50%)",
            width: 400,
            bgcolor: "background.paper",
            border: "2px solid #000",
            boxShadow: 24,
            p: 4,
          }}
        >
          <form onSubmit={(e) => handleSubmit(e)}>
            <Stack direction="column" spacing={2} alignItems="center">
              <Typography id="modal-modal-title" variant="h6" component="h2">
                {transactionModalContext.currentTransaction != null &&
                transactionModalContext.currentTransaction.id > 0
                  ? "Edit transaction"
                  : "Add new transaction"}
              </Typography>
              <LocalizationProvider dateAdapter={AdapterDayjs}>
                <DatePicker
                  label="Date"
                  sx={{ width: 300 }}
                  onChange={(e) =>
                    // updateTransaction({...transactionModalContext.currentTransaction, date: (e?.toDate() ?? new Date())})
                    transactionModalContext.modifyDate(e?.toDate())
                  }
                  value={dayjs(transactionModalContext.currentTransaction?.date) ?? new Dayjs()}
                />
              </LocalizationProvider>
              <TextField
                id="outlined-basic"
                sx={{ width: 300 }}
                label="Description"
                variant="outlined"
                value={transactionModalContext.currentTransaction?.description}
                onChange={(e) =>
                  transactionModalContext.modifyDescription(e.target.value)
                }
              />
              <TextField
                id="outlined-number"
                label="Amount"
                sx={{ width: 300 }}
                type="number"
                value={transactionModalContext.currentTransaction?.amount}
                onChange={(e) =>
                  transactionModalContext.modifyAmount(Number(e.target.value))
                }
              />
              <Autocomplete
                disablePortal
                options={categoryContext.categories}
                getOptionLabel={(category) => category.description || ""}
                isOptionEqualToValue={(option, value) => option.id === value.id}
                value={categoryContext.categories.find((c) => c.id == transactionModalContext.currentTransaction?.categoryId)}
                onChange={(_event, category) =>
                  transactionModalContext.modifyCategory(category)
                }
                sx={{ width: 300 }}
                renderInput={(params) => (
                  <TextField {...params} label="Category" />
                )}
              />
              <Autocomplete
                disablePortal
                options={accountContext.accounts}
                getOptionLabel={(account) => account.name || ""}
                isOptionEqualToValue={(option, value) => option.id === value.id}
                value={accountContext.accounts.find((a) => a.id == transactionModalContext.currentTransaction?.accountId)}
                onChange={(_event, account) =>
                  transactionModalContext.modifyAccount(account)
                }
                sx={{ width: 300 }}
                renderInput={(params) => (
                  <TextField {...params} label="Account" />
                )}
              />
              <Button variant="contained" type="submit" sx={{ width: 150 }}>
                Save
              </Button>
            </Stack>
          </form>
        </Box>
      </Modal>
    </>
  );
}
