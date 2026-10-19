import { useState } from "react";
import { Box, IconButton, Stack, TableCell, TableRow, Tooltip, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { DeleteOutlineRounded, EditOutlined } from "@mui/icons-material";
import Transaction from "../models/Transaction";
import { useTableContext } from "../stores/TableContext";
import { useTransactionModal } from "../stores/TransactionModalContext";
import { useTransactions } from "../stores/TransactionContext";
import ConfirmDeleteDialog from "./ConfirmDeleteDialog";
import { formatSignedCurrency } from "../utilities/format.utilities";
import { useSuccessMessage } from "../stores/SuccessMessageContext";

interface ExpenseTableRowProps {
  transaction: Transaction;
}

export default function ExpenseTableRow({ transaction }: ExpenseTableRowProps) {
  const a = useTheme().app;
  const tableContext = useTableContext();
  const transactionModalContext = useTransactionModal();
  const transactionContext = useTransactions();
  const [deleteDialogOpen, setDeleteDialogOpen] = useState<boolean>(false);
  const { showSuccess } = useSuccessMessage();

  const date = new Date(transaction.date);
  const isIncome = transaction.amount > 0;

  const renderCell = (columnId: string) => {
    switch (columnId) {
      case "date":
        return (
          <Box>
            <Typography sx={{ fontSize: "0.86rem", fontWeight: 600, color: a.text, whiteSpace: "nowrap" }}>
              {date.toLocaleDateString("it-IT", { day: "2-digit", month: "short", year: "numeric" })}
            </Typography>
            <Typography sx={{ fontSize: "0.72rem", color: a.textMuted }}>
              {date.toLocaleDateString("it-IT", { weekday: "long" })}
            </Typography>
          </Box>
        );
      case "description":
        return (
          <Typography sx={{ fontSize: "0.88rem", fontWeight: 500, color: a.text }}>
            {transaction.description || "—"}
          </Typography>
        );
      case "category":
        return (
          <Box
            component="span"
            sx={{
              display: "inline-flex",
              alignItems: "center",
              gap: 0.75,
              px: 1,
              py: 0.3,
              borderRadius: `${Math.max(a.radiusControl - 2, 4)}px`,
              backgroundColor: a.surfaceAlt,
              color: a.textSecondary,
              fontSize: "0.76rem",
              fontWeight: 600,
              maxWidth: "100%",
              whiteSpace: "nowrap",
              overflow: "hidden",
              textOverflow: "ellipsis",
            }}
          >
            {transaction.category || "Senza categoria"}
          </Box>
        );
      case "account":
        return <Typography sx={{ fontSize: "0.84rem", color: a.textSecondary }}>{transaction.account}</Typography>;
      case "amount":
        return (
          <Typography
            sx={{
              fontSize: "0.9rem",
              fontWeight: 700,
              color: isIncome ? a.income : a.text,
              whiteSpace: "nowrap",
              fontVariantNumeric: "tabular-nums",
            }}
          >
            {formatSignedCurrency(transaction.amount)}
          </Typography>
        );
      case "actions":
        return (
          <Stack
            className="row-actions"
            direction="row"
            spacing={0.25}
            justifyContent="flex-end"
            sx={{ opacity: { xs: 1, md: 0 }, transition: "opacity .15s ease" }}
          >
            <Tooltip title="Modifica">
              <IconButton
                size="small"
                aria-label="Modifica transazione"
                onClick={() => transactionModalContext.openTransactionModal(transaction)}
                sx={{ color: a.textSecondary }}
              >
                <EditOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Elimina">
              <IconButton
                size="small"
                aria-label="Elimina transazione"
                onClick={() => setDeleteDialogOpen(true)}
                sx={{ color: a.textSecondary, "&:hover": { color: a.expense } }}
              >
                <DeleteOutlineRounded fontSize="small" />
              </IconButton>
            </Tooltip>
          </Stack>
        );
      default:
        return null;
    }
  };

  return (
    <>
      <TableRow
        hover
        tabIndex={-1}
        sx={{
          "&:hover .row-actions, &:focus-within .row-actions": { opacity: 1 },
          "&.MuiTableRow-hover:hover": { backgroundColor: a.surfaceAlt },
        }}
      >
        {tableContext.columns.map((column) => (
          <TableCell key={column.id} align={column.align}>
            {renderCell(column.id)}
          </TableCell>
        ))}
      </TableRow>
      <ConfirmDeleteDialog
        open={deleteDialogOpen}
        onClose={() => setDeleteDialogOpen(false)}
        onConfirm={() => {
          transactionContext.deleteTransaction(transaction);
          showSuccess("Transazione eliminata");
        }}
        message={
          <>
            Vuoi eliminare &ldquo;{transaction.description || "questa transazione"}&rdquo; ({formatSignedCurrency(transaction.amount)})?
            L&apos;operazione non può essere annullata.
          </>
        }
      />
    </>
  );
}
