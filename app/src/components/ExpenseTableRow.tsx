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
import { useTheme } from "@mui/material/styles";
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
  const theme = useTheme();
  const c = theme.palette.custom;
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
          {value.toLocaleString("it-IT", {
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
                <IconButton
                  aria-label="Azioni transazione"
                  aria-controls={isActionsMenuOpen ? `transaction-actions-${transaction.id}` : undefined}
                  aria-expanded={isActionsMenuOpen ? "true" : undefined}
                  aria-haspopup="true"
                  onClick={onOpenActions}
                  sx={{
                    color: c.actionButtonColor,
                    border: `1px solid ${c.actionButtonBorder}`,
                    backgroundColor: c.actionButtonBackground,
                    "&:hover": {
                      color: theme.palette.text.secondary,
                      backgroundColor: c.actionButtonHover,
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
