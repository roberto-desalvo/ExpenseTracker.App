import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  TableCell,
  TableRow,
  Typography,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import Transaction from "../models/Transaction";
import TableColumn, { useTableContext } from "../stores/TableContext";
import {
  DeleteOutlineRounded,
  EditRounded,
} from "@mui/icons-material";
import { useState } from "react";
import { useTransactionModal } from "../stores/TransactionModalContext";
import { useTransactions } from "../stores/TransactionContext";
import RowActionsMenu from "./RowActionsMenu";

interface ExpenseTableRowProps {
  transaction: Transaction;
}

export default function ExpenseTableRow({ transaction }: ExpenseTableRowProps) {
  const theme = useTheme();
  const c = theme.palette.custom;
  const tableContext = useTableContext();
  const transactionModalContext = useTransactionModal();
  const transactionContext = useTransactions();
  const [deleteDialogOpen, setDeleteDialogOpen] = useState<boolean>(false);

  const getValueAsString = (column: TableColumn, value: unknown) => {
    return column.format && typeof value === "number"
      ? column.format(value)
      : column.id === "date" && typeof value === "string"
      ? new Date(value).toLocaleDateString()
      : column.id === "date" && value instanceof Date
      ? value.toLocaleDateString()
      : String(value);
  };

  const getCellStyle = (column: TableColumn, _value: unknown) => {
    return {
      color: theme.palette.text.primary,
      fontWeight: column.id === "amount" ? 700 : 400,
      borderBottom: "none",
      backgroundColor: c.rowBackground,
      py: 1.5,
    };
  };

  const renderCellContent = (column: TableColumn, value: unknown) => {
    if (column.id === "date") {
      return (
        <Box>
          <Typography sx={{ color: theme.palette.text.primary, fontWeight: 500, fontSize: "0.88rem" }}>
            {getValueAsString(column, value)}
          </Typography>
        </Box>
      );
    }

    if (column.id === "category" || column.id === "account") {
      return (
        <Box
          component="span"
          sx={{
            display: "inline-flex",
            alignItems: "center",
            px: 1.2,
            py: 0.4,
            borderRadius: "6px",
            backgroundColor: c.badgeBackground,
            border: `1px solid ${c.badgeBorder}`,
            color: c.badgeText,
            fontWeight: 500,
            fontSize: "0.8rem",
          }}
        >
          {getValueAsString(column, value)}
        </Box>
      );
    }

    if (column.id === "amount" && typeof value === "number") {
      return (
        <Typography sx={{ color: value > 0 ? c.amountPositive : c.amountNegative, fontWeight: 600, fontSize: "0.92rem" }}>
          {Math.abs(value).toLocaleString("it-IT", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
          })}
          {" EUR"}
        </Typography>
      );
    }

    return (
      <Typography sx={{ color: theme.palette.text.secondary, fontWeight: 400, fontSize: "0.9rem" }}>
        {getValueAsString(column, value)}
      </Typography>
    );
  };

  const onEdit = () => {
    transactionModalContext.openTransactionModal(transaction);
  };

  const onDelete = () => {
    setDeleteDialogOpen(true);
  };

  const onConfirmDelete = () => {
    transactionContext.deleteTransaction(transaction);
    setDeleteDialogOpen(false);
  };

  const onCancelDelete = () => {
    setDeleteDialogOpen(false);
  };

  return (
    <>
      <TableRow
        hover
        role="checkbox"
        tabIndex={-1}
        key={transaction.id}
        sx={{
          "& td:first-of-type": {
            borderTopLeftRadius: 8,
            borderBottomLeftRadius: 8,
          },
          "& td:last-of-type": {
            borderTopRightRadius: 8,
            borderBottomRightRadius: 8,
          },
          "&:hover td": {
            backgroundColor: c.rowHover,
          },
        }}
      >
        {tableContext.columns.map((column) => {
          if (column.id === "actions") {
            return (
              <TableCell
                key={column.id}
                align={column.align}
                sx={getCellStyle(column, "")}
              >
                <RowActionsMenu
                  rowId={transaction.id}
                  ariaLabel="Azioni transazione"
                  actions={[
                    {
                      label: "Modifica",
                      icon: <EditRounded fontSize="small" />,
                      onClick: onEdit,
                    },
                    {
                      label: "Elimina",
                      icon: <DeleteOutlineRounded fontSize="small" />,
                      onClick: onDelete,
                    },
                  ]}
                />
              </TableCell>
            );
          }

          const value = transaction[column.id];
          return (
            <TableCell
              key={column.id}
              align={column.align}
              sx={getCellStyle(column, value)}
            >
              {renderCellContent(column, value)}
            </TableCell>
          );
        })}
      </TableRow>
      <Dialog
        open={deleteDialogOpen}
        onClose={onCancelDelete}
        aria-labelledby={`delete-transaction-title-${transaction.id}`}
        aria-describedby={`delete-transaction-description-${transaction.id}`}
      >
        <DialogTitle id={`delete-transaction-title-${transaction.id}`}>
          Conferma eliminazione
        </DialogTitle>
        <DialogContent>
          <DialogContentText id={`delete-transaction-description-${transaction.id}`}>
            Vuoi eliminare questa transazione? L&apos;operazione non può essere annullata.
          </DialogContentText>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={onCancelDelete} color="inherit">
            Annulla
          </Button>
          <Button onClick={onConfirmDelete} color="error" variant="contained">
            Elimina
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
