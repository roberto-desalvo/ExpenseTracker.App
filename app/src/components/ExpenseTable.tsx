import * as React from "react";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TablePagination from "@mui/material/TablePagination";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
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
        background:
          "linear-gradient(180deg, rgba(255,255,255,0.98) 0%, rgba(248,250,252,0.98) 100%)",
        display: "flex",
        flexDirection: "column",
        borderRadius: 6,
        border: "1px solid rgba(148, 163, 184, 0.18)",
        boxShadow: "0 22px 60px rgba(15, 23, 42, 0.22)",
      }}
    >
      <Box
        sx={{
          px: { xs: 2, md: 3 },
          pt: { xs: 2, md: 2.5 },
          pb: 1.5,
          borderBottom: "1px solid rgba(148, 163, 184, 0.14)",
          background:
            "linear-gradient(135deg, rgba(241,245,249,0.85) 0%, rgba(255,255,255,0.65) 100%)",
        }}
      >
        <Stack
          direction={{ xs: "column", md: "row" }}
          spacing={1.5}
          alignItems={{ xs: "flex-start", md: "center" }}
          justifyContent="space-between"
        >
          <Box>
            <Typography
              variant="h6"
              sx={{ color: "#0f172a", fontWeight: 700, letterSpacing: "-0.02em" }}
            >
              Transazioni
            </Typography>
            <Typography variant="body2" sx={{ color: "#64748b" }}>
              Elenco aggiornato con filtri, paging e azioni rapide.
            </Typography>
          </Box>
          <Chip
            label={`${transactionContext.totalCount} movimenti`}
            sx={{
              backgroundColor: "rgba(15, 23, 42, 0.06)",
              color: "#0f172a",
              fontWeight: 700,
              borderRadius: "999px",
            }}
          />
        </Stack>
      </Box>
      <TableContainer sx={{ flex: 1, minHeight: 0, overflow: "auto", px: { xs: 1.5, md: 2 }, py: 1.5 }}>
        <Table
          stickyHeader
          sx={{
            borderCollapse: "separate",
            borderSpacing: "0 10px",
            minWidth: 860,
          }}
        >
          <TableHead>
            <TableRow>
              {tableContext.columns.map((column) => (
                <TableCell
                  key={column.id}
                  align={column.align}
                  sx={{
                    width: column.minWidth,
                    background: "transparent",
                    color: "#64748b",
                    textTransform: "uppercase",
                    fontSize: "0.72rem",
                    fontWeight: 800,
                    letterSpacing: "0.08em",
                    borderBottom: "none",
                    px: 2,
                    py: 0.5,
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
        sx={{
          borderTop: "1px solid rgba(148, 163, 184, 0.14)",
          px: { xs: 1, md: 2 },
          backgroundColor: "rgba(248, 250, 252, 0.92)",
          ".MuiTablePagination-toolbar": {
            minHeight: 64,
            color: "#475569",
          },
          ".MuiTablePagination-selectLabel, .MuiTablePagination-displayedRows": {
            fontWeight: 600,
          },
        }}
      />
    </Paper>
  );
}
