import {
  Box,
  IconButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  TableCell,
  TableRow,
  Typography,
} from "@mui/material";
import Transaction from "../models/Transaction";
import TableColumn, { useTableContext } from "../stores/TableContext";
import {
  DeleteOutlineRounded,
  EditRounded,
  MoreVertRounded,
} from "@mui/icons-material";
import { useState } from "react";
import { useTransactionModal } from "../stores/TransactionModalContext";
import { useTransactions } from "../stores/TransactionContext";

interface ExpenseTableRowProps {
  transaction: Transaction;
}

export default function ExpenseTableRow({ transaction }: ExpenseTableRowProps) {
  const tableContext = useTableContext();
  const transactionModalContext = useTransactionModal();
  const transactionContext = useTransactions();
  const [actionsAnchorEl, setActionsAnchorEl] = useState<null | HTMLElement>(null);

  const isActionsMenuOpen = Boolean(actionsAnchorEl);

  const getValueAsString = (column: TableColumn, value: unknown) => {
    return column.format && typeof value === "number"
      ? column.format(value)
      : column.id === "date" && typeof value === "string"
      ? new Date(value).toLocaleDateString()
      : column.id === "date" && value instanceof Date
      ? value.toLocaleDateString()
      : String(value);
  };

  const getCellStyle = (column: TableColumn, value: unknown) => {
    return {
      color:
        column.id === "amount" && typeof value === "number"
          ? value > 0
            ? "#166534"
            : "#b91c1c"
          : "#0f172a",
      fontWeight: column.id === "amount" ? 700 : 500,
      borderBottom: "none",
      backgroundColor: "#ffffff",
      py: 2,
    };
  };

  const renderCellContent = (column: TableColumn, value: unknown) => {
    if (column.id === "date") {
      return (
        <Box>
          <Typography sx={{ color: "#0f172a", fontWeight: 700, fontSize: "0.94rem" }}>
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
            px: 1.25,
            py: 0.45,
            borderRadius: "999px",
            backgroundColor: "rgba(148, 163, 184, 0.12)",
            color: "#334155",
            fontWeight: 700,
            fontSize: "0.82rem",
          }}
        >
          {getValueAsString(column, value)}
        </Box>
      );
    }

    if (column.id === "amount" && typeof value === "number") {
      return (
        <Typography sx={{ color: value > 0 ? "#166534" : "#b91c1c", fontWeight: 800 }}>
          {value.toLocaleString("it-IT", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
          })}
          {" "}
          EUR
        </Typography>
      );
    }

    return (
      <Typography sx={{ color: "#0f172a", fontWeight: 500 }}>
        {getValueAsString(column, value)}
      </Typography>
    );
  };

  const onEdit = () => {
    transactionModalContext.setTransaction(transaction);
    transactionModalContext.openTransactionModal();
    setActionsAnchorEl(null);
  };

  const onDelete = () => {
    transactionContext.deleteTransaction(transaction);
    setActionsAnchorEl(null);
  };

  const onOpenActions = (event: React.MouseEvent<HTMLElement>) => {
    setActionsAnchorEl(event.currentTarget);
  };

  const onCloseActions = () => {
    setActionsAnchorEl(null);
  };

  return (
    <>
      <TableRow
        hover
        role="checkbox"
        tabIndex={-1}
        key={transaction.id}
        sx={{
          fontWeight: 500,
          "& td:first-of-type": {
            borderTopLeftRadius: 18,
            borderBottomLeftRadius: 18,
          },
          "& td:last-of-type": {
            borderTopRightRadius: 18,
            borderBottomRightRadius: 18,
          },
          "&:hover td": {
            backgroundColor: "#f8fafc",
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
                <IconButton
                  aria-label="Azioni transazione"
                  aria-controls={isActionsMenuOpen ? `transaction-actions-${transaction.id}` : undefined}
                  aria-expanded={isActionsMenuOpen ? "true" : undefined}
                  aria-haspopup="true"
                  onClick={onOpenActions}
                  sx={{
                    border: "1px solid rgba(148, 163, 184, 0.24)",
                    backgroundColor: "rgba(248, 250, 252, 0.95)",
                    "&:hover": {
                      backgroundColor: "rgba(226, 232, 240, 0.95)",
                    },
                  }}
                >
                  <MoreVertRounded />
                </IconButton>
                <Menu
                  id={`transaction-actions-${transaction.id}`}
                  anchorEl={actionsAnchorEl}
                  open={isActionsMenuOpen}
                  onClose={onCloseActions}
                >
                  <MenuItem onClick={onEdit}>
                    <ListItemIcon>
                      <EditRounded fontSize="small" />
                    </ListItemIcon>
                    <ListItemText>Modifica</ListItemText>
                  </MenuItem>
                  <MenuItem onClick={onDelete}>
                    <ListItemIcon>
                      <DeleteOutlineRounded fontSize="small" />
                    </ListItemIcon>
                    <ListItemText>Elimina</ListItemText>
                  </MenuItem>
                </Menu>
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
    </>
  );
}
