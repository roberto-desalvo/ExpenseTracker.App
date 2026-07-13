import { Box, ButtonBase, Stack, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { useTableContext } from "../stores/TableContext";
import { useTransactions } from "../stores/TransactionContext";

interface TransactionsSummaryBarProps {
  showChips?: boolean;
}

export default function TransactionsSummaryBar({ showChips = true }: TransactionsSummaryBarProps) {
  const theme = useTheme();
  const c = theme.palette.custom;
  const tableContext = useTableContext();
  const transactionContext = useTransactions();

  const formatAmount = (value: number) =>
    value.toLocaleString("it-IT", {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });

  const isMovementActive = (movementType: "incomes" | "outcomes") =>
    tableContext.movementType === movementType;

  if (!showChips) {
    return null;
  }

  return (
    <Stack
      direction="row"
      spacing={1.25}
      flexWrap="wrap"
      useFlexGap
      alignItems="stretch"
      justifyContent={{ xs: "flex-start", md: "flex-end" }}
      sx={{ px: 1 }}
    >
      <ButtonBase
        onClick={() => tableContext.modifyMovementType("incomes")}
        sx={{
          borderRadius: 1.5,
          border: `1px solid ${isMovementActive("incomes") ? c.amountPositive : c.badgeBorder}`,
          backgroundColor: isMovementActive("incomes")
            ? (theme.palette.mode === "light" ? "rgba(22,163,74,0.09)" : "rgba(74,222,128,0.12)")
            : c.badgeBackground,
          px: 1.25,
          py: 0.8,
          minWidth: 128,
          textAlign: "left",
          justifyContent: "flex-start",
          boxShadow: isMovementActive("incomes") ? `0 0 0 1px ${c.amountPositive}` : "none",
          "&:hover": {
            backgroundColor:
              theme.palette.mode === "light" ? "rgba(22,163,74,0.12)" : "rgba(74,222,128,0.16)",
          },
        }}
      >
        <Stack spacing={0.1}>
          <Typography sx={{ fontSize: "0.68rem", color: c.badgeText, textTransform: "uppercase", letterSpacing: "0.06em" }}>
            Entrate
          </Typography>
          <Typography sx={{ fontSize: "0.85rem", fontWeight: 700, color: c.amountPositive }}>
            + {formatAmount(transactionContext.totalIncomes)} EUR
          </Typography>
        </Stack>
      </ButtonBase>

      <ButtonBase
        onClick={() => tableContext.modifyMovementType("outcomes")}
        sx={{
          borderRadius: 1.5,
          border: `1px solid ${isMovementActive("outcomes") ? c.amountNegative : c.badgeBorder}`,
          backgroundColor: isMovementActive("outcomes")
            ? (theme.palette.mode === "light" ? "rgba(220,38,38,0.08)" : "rgba(248,113,113,0.11)")
            : c.badgeBackground,
          px: 1.25,
          py: 0.8,
          minWidth: 128,
          textAlign: "left",
          justifyContent: "flex-start",
          boxShadow: isMovementActive("outcomes") ? `0 0 0 1px ${c.amountNegative}` : "none",
          "&:hover": {
            backgroundColor:
              theme.palette.mode === "light" ? "rgba(220,38,38,0.12)" : "rgba(248,113,113,0.15)",
          },
        }}
      >
        <Stack spacing={0.1}>
          <Typography sx={{ fontSize: "0.68rem", color: c.badgeText, textTransform: "uppercase", letterSpacing: "0.06em" }}>
            Uscite
          </Typography>
          <Typography sx={{ fontSize: "0.85rem", fontWeight: 700, color: c.amountNegative }}>
            - {formatAmount(transactionContext.totalOutcomes)} EUR
          </Typography>
        </Stack>
      </ButtonBase>

      <Box
        sx={{
          borderRadius: 1.5,
          backgroundColor: theme.palette.mode === "light" ? "rgba(59,130,246,0.10)" : "rgba(59,130,246,0.18)",
          px: 1.25,
          py: 0.8,
          minWidth: 132,
          border: `1px solid ${theme.palette.mode === "light" ? "rgba(59,130,246,0.35)" : "rgba(147,197,253,0.45)"}`,
        }}
      >
        <Stack spacing={0.1}>
          <Typography sx={{ fontSize: "0.68rem", color: c.badgeText, textTransform: "uppercase", letterSpacing: "0.06em" }}>
            Bilancio
          </Typography>
          <Typography
            sx={{
              fontSize: "0.85rem",
              fontWeight: 700,
              color: transactionContext.totalNet >= 0 ? c.amountPositive : c.amountNegative,
            }}
          >
            {transactionContext.totalNet >= 0 ? "+" : "-"} {formatAmount(Math.abs(transactionContext.totalNet))} EUR
          </Typography>
        </Stack>
      </Box>
    </Stack>
  );
}