import Chip from "@mui/material/Chip";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useTheme } from "@mui/material/styles";
import ExpenseTableRow from "./ExpenseTableRow";
import DataTableBase from "./DataTableBase";
import { useTableContext } from "../stores/TableContext";
import { useTransactions } from "../stores/TransactionContext";
import { useTransactionModal } from "../stores/TransactionModalContext";
import { Add } from "@mui/icons-material";
import { IconButton, Tooltip } from "@mui/material";

export default function ExpenseTable() {
  const theme = useTheme();
  const c = theme.palette.custom;
  const tableContext = useTableContext();
  const transactionContext = useTransactions();
  const transactionModalContext = useTransactionModal();
  const filteredTransactions = tableContext.getFilteredTransactions();

  const formatAmount = (value: number) =>
    value.toLocaleString("it-IT", {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });

  const isMovementActive = (movementType: "incomes" | "outcomes") =>
    tableContext.movementType === movementType;

  return (
    <DataTableBase
      title="Transazioni"
      columns={tableContext.columns as unknown as import("./DataTableBase").DataTableColumn[]}
      rows={filteredTransactions}
      isLoading={transactionContext.isLoading}
      isEmpty={!transactionContext.isLoading && filteredTransactions.length === 0}
      emptyMessage="Nessuna transazione trovata"
      emptySubtext="Modifica i filtri o aggiungi una nuova transazione"
      page={tableContext.page}
      pageSize={tableContext.pageSize}
      totalCount={transactionContext.totalCount}
      onPageChange={(_event, newPage) => tableContext.modifyPage(newPage)}
      onPageSizeChange={(event) => tableContext.modifyPageSize(+event.target.value)}
      rowsPerPageOptions={[10, 25, 100]}
      renderRow={(t) => <ExpenseTableRow transaction={t} key={t.id} />}
      headerRightContent={
        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap alignItems="center">
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
              boxShadow: isMovementActive("incomes") ? `0 0 0 1px ${c.amountPositive}` : "none",
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
              boxShadow: isMovementActive("outcomes") ? `0 0 0 1px ${c.amountNegative}` : "none",
            }}
          />
          <Typography
            component="span"
            sx={{ color: c.badgeText, fontWeight: 700, fontSize: "0.95rem", alignSelf: "center", px: 0.25 }}
          >
            =
          </Typography>
          <Chip
            label={`${transactionContext.totalNet >= 0 ? "+" : "-"} ${formatAmount(Math.abs(transactionContext.totalNet))} EUR`}
            sx={{
              backgroundColor: theme.palette.mode === "light" ? "rgba(59,130,246,0.12)" : "rgba(59,130,246,0.2)",
              color: transactionContext.totalNet >= 0 ? c.amountPositive : c.amountNegative,
              fontWeight: 700,
              fontSize: "0.78rem",
              borderRadius: "999px",
              border: `1px solid ${theme.palette.mode === "light" ? "rgba(59,130,246,0.35)" : "rgba(147,197,253,0.45)"}`,
            }}
          />
          <Tooltip title="Nuova transazione">
            <IconButton
              size="small"
              onClick={() => transactionModalContext.openTransactionModal()}
              sx={{
                color: c.actionButtonColor,
                border: `1px solid ${c.actionButtonBorder}`,
                backgroundColor: c.actionButtonBackground,
                "&:hover": { backgroundColor: c.actionButtonHover },
              }}
            >
              <Add fontSize="small" />
            </IconButton>
          </Tooltip>
        </Stack>
      }
    />
  );
}
