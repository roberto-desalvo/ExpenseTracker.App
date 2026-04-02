import { TableCell, TableRow } from "@mui/material";
import Transaction from "../models/Transaction";
import TableColumn, { useTableContext } from "../stores/TableContext";
import {
  DeleteOutlineRounded,
  EditRounded,
} from "@mui/icons-material";
import { useTransactionModal } from "../stores/TransactionModalContext";
import { useTransactions } from "../stores/TransactionContext";

interface ExpenseTableRowProps {
  transaction: Transaction;
}

export default function ExpenseTableRow({ transaction }: ExpenseTableRowProps) {
  const tableContext = useTableContext();
  const transactionModalContext = useTransactionModal();
  const transactionContext = useTransactions();

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
            ? "rgb(76, 175, 80)"
            : "rgb(244, 67, 54)"
          : "black",
      fontWeight: 500,
      textTransform: "uppercase",
    };
  };

  const onDelete = () => {
    transactionContext.deleteTransaction(transaction);
  }

  return (
    <>
      <TableRow
        hover
        role="checkbox"
        tabIndex={-1}
        key={transaction.id}
        sx={{ fontWeight: 500 }}
      >
        {tableContext.columns.map((column) => {
          if (column.id === "edit") {
            function edit(): void {
              transactionModalContext.setTransaction(transaction);
              transactionModalContext.openTransactionModal();
            }

            return (
              <TableCell
                key={column.id}
                align={column.align}
                sx={getCellStyle(column, "")}
              >
                <EditRounded onClick={() => edit()}/>
              </TableCell>
            );
          }
          if (column.id === "delete") {
            return (
              <TableCell
                key={column.id}
                align={column.align}
                sx={getCellStyle(column, "")}
              >
                <DeleteOutlineRounded onClick={() => onDelete()}/>
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
              {getValueAsString(column, value)}
            </TableCell>
          );
        })}
      </TableRow>
    </>
  );
}
