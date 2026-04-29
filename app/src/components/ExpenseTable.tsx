import * as React from "react";
import Paper from "@mui/material/Paper";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TablePagination from "@mui/material/TablePagination";
import TableRow from "@mui/material/TableRow";
import ExpenseTableRow from "./ExpenseTableRow";
import { useTableContext } from "../stores/TableContext";
import { useTransactions } from "../stores/TransactionContext";

export default function ExpenseTable() {
  const tableContext = useTableContext();
  const transactionContext = useTransactions();
  const filteredTransactions = tableContext.getFilteredTransactions();

  const handleChangePage = (_event: unknown, newPage: number) => {
    tableContext.modifyPage(newPage);
  };

  const handleChangeRowsPerPage = (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    tableContext.modifyPageSize(+event.target.value);
  };

  return (
    <Paper
      sx={{
        height: "100%",
        width: "100%",
        overflow: "hidden",
        background: "white",
        display: "flex",
        flexDirection: "column",
      }}
    >
      <TableContainer sx={{ flex: 1, minHeight: 0, overflow: "auto" }}>
        <Table stickyHeader>
          <TableHead>
            <TableRow>
              {tableContext.columns.map((column) => (
                <TableCell
                  key={column.id}
                  align={column.align}
                  sx={{
                    width: column.minWidth,
                    background: "rgb(245 245 245)",
                    color: "rgb(97 97 97)",
                    textTransaform: "uppercase",
                  }}
                >
                  {column.label}
                </TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {filteredTransactions.map((t) => (
              <ExpenseTableRow transaction={t} key={t.id} />
            ))}
          </TableBody>
        </Table>
      </TableContainer>
      <TablePagination
        rowsPerPageOptions={[10, 25, 100]}
        component="div"
        count={transactionContext.totalCount}
        rowsPerPage={tableContext.pageSize}
        page={tableContext.page}
        onPageChange={handleChangePage}
        onRowsPerPageChange={handleChangeRowsPerPage}
      />
    </Paper>
  );
}
