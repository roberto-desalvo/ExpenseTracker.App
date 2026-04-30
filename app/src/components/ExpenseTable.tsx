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
import { useTheme } from "@mui/material/styles";
import ExpenseTableRow from "./ExpenseTableRow";
import { useTableContext } from "../stores/TableContext";
import { useTransactions } from "../stores/TransactionContext";

export default function ExpenseTable() {
  const theme = useTheme();
  const c = theme.palette.custom;
  const tableContext = useTableContext();
  const transactionContext = useTransactions();
  const filteredTransactions = tableContext.getFilteredTransactions();

  const formatAmount = (value: number) =>
    value.toLocaleString("it-IT", {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });

  const isMovementActive = (movementType: "incomes" | "outcomes") =>
    tableContext.movementType === movementType;

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
        background: "transparent",
        display: "flex",
        flexDirection: "column",
        borderRadius: 4,
        border: `1px solid ${theme.palette.divider}`,
      }}
    >
      <Box
        sx={{
          px: { xs: 2, md: 3 },
          pt: { xs: 2, md: 2 },
          pb: 1.5,
          borderBottom: `1px solid ${theme.palette.divider}`,
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
              sx={{ color: "text.primary", fontWeight: 600, letterSpacing: "-0.01em", fontSize: "1rem" }}
            >
              Transazioni
            </Typography>
          </Box>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <Chip
              label={`${transactionContext.totalCount} movimenti`}
              sx={{
                backgroundColor: c.badgeBackground,
                color: c.badgeText,
                fontWeight: 600,
                fontSize: "0.78rem",
                borderRadius: "999px",
                border: `1px solid ${c.badgeBorder}`,
              }}
            />
            <Chip
              label={`+ ${formatAmount(transactionContext.totalIncomes)} EUR`}
              onClick={() => tableContext.modifyMovementType("incomes")}
              sx={{
                backgroundColor: c.badgeBackground,
                color: c.amountPositive,
                fontWeight: 700,
                fontSize: "0.78rem",
                borderRadius: "999px",
                border: `1px solid ${isMovementActive("incomes") ? c.amountPositive : c.badgeBorder}`,
                cursor: "pointer",
                boxShadow: isMovementActive("incomes")
                  ? `0 0 0 1px ${c.amountPositive}`
                  : "none",
              }}
            />
            <Chip
              label={`- ${formatAmount(transactionContext.totalOutcomes)} EUR`}
              onClick={() => tableContext.modifyMovementType("outcomes")}
              sx={{
                backgroundColor: c.badgeBackground,
                color: c.amountNegative,
                fontWeight: 700,
                fontSize: "0.78rem",
                borderRadius: "999px",
                border: `1px solid ${isMovementActive("outcomes") ? c.amountNegative : c.badgeBorder}`,
                cursor: "pointer",
                boxShadow: isMovementActive("outcomes")
                  ? `0 0 0 1px ${c.amountNegative}`
                  : "none",
              }}
            />
            <Typography
              component="span"
              sx={{
                color: c.badgeText,
                fontWeight: 700,
                fontSize: "0.95rem",
                lineHeight: 1,
                alignSelf: "center",
                px: 0.25,
              }}
            >
              =
            </Typography>
            <Chip
              label={`${transactionContext.totalNet >= 0 ? "+" : "-"} ${formatAmount(Math.abs(transactionContext.totalNet))} EUR`}
              sx={{
                backgroundColor: `${theme.palette.mode === "light" ? "rgba(59, 130, 246, 0.12)" : "rgba(59, 130, 246, 0.2)"}`,
                color:
                  transactionContext.totalNet >= 0
                    ? c.amountPositive
                    : c.amountNegative,
                fontWeight: 700,
                fontSize: "0.78rem",
                borderRadius: "999px",
                border: `1px solid ${theme.palette.mode === "light" ? "rgba(59, 130, 246, 0.35)" : "rgba(147, 197, 253, 0.45)"}`,
              }}
            />
          </Stack>
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
                    background: "background.default",
                    bgcolor: "background.default",
                    color: c.tableHeaderText,
                    textTransform: "uppercase",
                    fontSize: "0.68rem",
                    fontWeight: 700,
                    letterSpacing: "0.1em",
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
        labelRowsPerPage="Elementi per pagina"
        onPageChange={handleChangePage}
        onRowsPerPageChange={handleChangeRowsPerPage}
        sx={{
          borderTop: `1px solid ${theme.palette.divider}`,
          px: { xs: 1, md: 2 },
          color: "text.secondary",
          ".MuiTablePagination-toolbar": {
            minHeight: 52,
            color: theme.palette.text.secondary,
          },
          ".MuiTablePagination-selectLabel, .MuiTablePagination-displayedRows": {
            fontWeight: 500,
            color: theme.palette.text.secondary,
          },
          ".MuiTablePagination-select, .MuiTablePagination-selectIcon": {
            color: theme.palette.text.secondary,
          },
          ".MuiIconButton-root": {
            color: theme.palette.text.secondary,
            "&:hover": { color: theme.palette.text.primary },
          },
        }}
      />
    </Paper>
  );
}
