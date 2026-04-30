import {
  Autocomplete,
  Box,
  Button,
  Modal,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { useTransactionModal } from "../stores/TransactionModalContext";
import { useCategories } from "../stores/CategoryContext";
import { DatePicker, LocalizationProvider } from "@mui/x-date-pickers";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { useAccounts } from "../stores/AccountContext";
import dayjs from "dayjs";

export default function TransactionModal() {
  const theme = useTheme();
  const c = theme.palette.custom;
  const transactionModalContext = useTransactionModal();
  const categoryContext = useCategories();
  const accountContext = useAccounts();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await transactionModalContext.sendTransaction();
    transactionModalContext.closeTransactionModal();
  };

  const handleCancel = () => {
    transactionModalContext.closeTransactionModal();
  };

  return (
    <>
      <Modal
        open={transactionModalContext.transactionModalOpen}
        onClose={() => transactionModalContext.closeTransactionModal()}
        aria-labelledby="modal-modal-title"
        aria-describedby="modal-modal-description"
        slotProps={{
          backdrop: {
            sx: {
              backgroundColor:
                theme.palette.mode === "dark"
                  ? "rgba(2, 6, 23, 0.7)"
                  : "rgba(15, 23, 42, 0.35)",
              backdropFilter: "blur(2px)",
            },
          },
        }}
      >
        <Box
          sx={{
            position: "absolute",
            top: "50%",
            left: "50%",
            transform: "translate(-50%, -50%)",
            width: { xs: "calc(100% - 24px)", sm: 460 },
            maxWidth: 460,
            bgcolor: c.drawerBackground,
            border: `1px solid ${c.drawerBorder}`,
            borderRadius: "14px",
            boxShadow:
              theme.palette.mode === "dark"
                ? "0 16px 40px rgba(0,0,0,0.45)"
                : "0 16px 40px rgba(15,23,42,0.16)",
            p: { xs: 2, sm: 3 },
          }}
        >
          <form onSubmit={(e) => handleSubmit(e)}>
            <Stack direction="column" spacing={2}>
              <Typography id="modal-modal-title" variant="h6" component="h2" sx={{ fontWeight: 600 }}>
                {transactionModalContext.currentTransaction != null &&
                transactionModalContext.currentTransaction.id > 0
                  ? "Modifica transazione"
                  : "Nuova transazione"}
              </Typography>
              <LocalizationProvider dateAdapter={AdapterDayjs}>
                <DatePicker
                  label="Data"
                  sx={{ width: "100%" }}
                  onChange={(e) =>
                    transactionModalContext.modifyDate(e?.toDate())
                  }
                  value={
                    transactionModalContext.currentTransaction?.date
                      ? dayjs(transactionModalContext.currentTransaction.date)
                      : null
                  }
                />
              </LocalizationProvider>
              <TextField
                id="outlined-basic"
                sx={{ width: "100%" }}
                label="Descrizione"
                variant="outlined"
                value={transactionModalContext.currentTransaction?.description ?? ""}
                onChange={(e) =>
                  transactionModalContext.modifyDescription(e.target.value)
                }
              />
              <TextField
                id="outlined-number"
                label="Importo"
                sx={{ width: "100%" }}
                type="number"
                value={transactionModalContext.currentTransaction?.amount ?? ""}
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
                sx={{ width: "100%" }}
                renderInput={(params) => (
                  <TextField {...params} label="Categoria" />
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
                sx={{ width: "100%" }}
                renderInput={(params) => (
                  <TextField {...params} label="Conto" />
                )}
              />
              <Stack direction="row" spacing={1.25} justifyContent="flex-end" sx={{ pt: 1 }}>
                <Button
                  variant="outlined"
                  onClick={handleCancel}
                  sx={{
                    minWidth: 120,
                    borderColor: c.filterBorder,
                    color: theme.palette.text.secondary,
                    "&:hover": {
                      borderColor: c.drawerBorder,
                      backgroundColor: c.filterBackground,
                    },
                  }}
                >
                  Annulla
                </Button>
                <Button
                  variant="contained"
                  type="submit"
                  sx={{
                    minWidth: 120,
                    backgroundColor: c.accentColor,
                    color: theme.palette.mode === "dark" ? "#0f172a" : "#ffffff",
                    "&:hover": {
                      backgroundColor: theme.palette.mode === "dark" ? "#bef264" : "#4d7c0f",
                    },
                  }}
                >
                  Salva
                </Button>
              </Stack>
            </Stack>
          </form>
        </Box>
      </Modal>
    </>
  );
}
