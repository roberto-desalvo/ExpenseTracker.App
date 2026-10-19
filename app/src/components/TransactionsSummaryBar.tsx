import { ReactNode } from "react";
import { Box, ButtonBase, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { CheckCircleRounded, NorthEastRounded, SouthWestRounded, SwapVertRounded } from "@mui/icons-material";
import { MovementType, useTableContext } from "../stores/TableContext";
import { useTransactions } from "../stores/TransactionContext";
import { formatCurrency, formatSignedCurrency } from "../utilities/format.utilities";

/**
 * Totali del periodo filtrato. Entrate e Uscite sono anche filtri: un click mostra
 * solo quel tipo di movimento, un secondo click torna a tutti.
 */
export default function TransactionsSummaryBar() {
  const a = useTheme().app;
  const tableContext = useTableContext();
  const transactions = useTransactions();

  const tile = (
    key: MovementType,
    label: string,
    value: string,
    icon: ReactNode,
    color: string,
    soft: string,
    clickable: boolean,
  ) => {
    const active = clickable && tableContext.movementType === key;

    return (
      <ButtonBase
        key={key}
        disabled={!clickable}
        onClick={clickable ? () => tableContext.modifyMovementType(key) : undefined}
        aria-pressed={clickable ? active : undefined}
        sx={{
          justifyContent: "flex-start",
          textAlign: "left",
          gap: 1.5,
          p: 2,
          borderRadius: `${a.radiusCard}px`,
          border: `1px solid ${active ? color : a.border}`,
          boxShadow: active ? `0 0 0 1px ${color}` : a.cardShadow,
          backgroundColor: a.surface,
          transition: "border-color .15s ease, box-shadow .15s ease",
          "&:hover": clickable ? { borderColor: color } : undefined,
          "&.Mui-disabled": { opacity: 1 },
        }}
      >
        <Box
          sx={{
            width: 38,
            height: 38,
            flexShrink: 0,
            borderRadius: `${Math.max(a.radiusControl, 6)}px`,
            display: "grid",
            placeItems: "center",
            backgroundColor: soft,
            color,
          }}
        >
          {icon}
        </Box>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Typography sx={{ fontSize: "0.78rem", fontWeight: 600, color: a.textSecondary }}>
            {label}
            {clickable && (
              <Box component="span" sx={{ color: a.textMuted, fontWeight: 500 }}>
                {active ? " · filtro attivo" : " · clicca per filtrare"}
              </Box>
            )}
          </Typography>
          <Typography sx={{ fontSize: "1.2rem", fontWeight: 750, color: a.text, letterSpacing: "-0.02em" }}>
            {value}
          </Typography>
        </Box>
        {active && <CheckCircleRounded sx={{ color, fontSize: 20 }} />}
      </ButtonBase>
    );
  };

  return (
    <Box sx={{ display: "grid", gap: 2, gridTemplateColumns: { xs: "1fr", sm: "repeat(3, 1fr)" } }}>
      {tile("incomes", "Entrate", formatCurrency(transactions.totalIncomes), <SouthWestRounded fontSize="small" />, a.income, a.incomeSoft, true)}
      {tile("outcomes", "Uscite", formatCurrency(transactions.totalOutcomes), <NorthEastRounded fontSize="small" />, a.expense, a.expenseSoft, true)}
      {tile("all", "Saldo del periodo", formatSignedCurrency(transactions.totalNet), <SwapVertRounded fontSize="small" />, a.primary, a.primarySoft, false)}
    </Box>
  );
}
