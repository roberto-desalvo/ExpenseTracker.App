import { Box, Skeleton, Stack, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { ReceiptLongRounded, SouthWestRounded, NorthEastRounded } from "@mui/icons-material";
import Transaction from "../../models/Transaction";
import { formatSignedCurrency } from "../../utilities/format.utilities";
import EmptyState from "../ui/EmptyState";

interface RecentTransactionsProps {
  items: Transaction[];
  loading: boolean;
  onSelect?: (transaction: Transaction) => void;
}

const formatDay = (value: Date | string) =>
  new Date(value).toLocaleDateString("it-IT", { day: "numeric", month: "short" });

/** Elenco compatto degli ultimi movimenti (feed stile app bancaria). */
export default function RecentTransactions({ items, loading, onSelect }: RecentTransactionsProps) {
  const a = useTheme().app;

  if (loading) {
    return (
      <Stack spacing={1.5}>
        {Array.from({ length: 6 }).map((_, i) => (
          <Stack key={i} direction="row" spacing={1.5} alignItems="center">
            <Skeleton variant="rounded" width={36} height={36} />
            <Box sx={{ flex: 1 }}>
              <Skeleton width="60%" />
              <Skeleton width="35%" />
            </Box>
            <Skeleton width={70} />
          </Stack>
        ))}
      </Stack>
    );
  }

  if (items.length === 0) {
    return <EmptyState icon={<ReceiptLongRounded />} title="Nessun movimento recente" text="I movimenti degli ultimi 90 giorni compariranno qui." />;
  }

  return (
    <Stack spacing={0.25}>
      {items.map((t) => {
        const isIncome = t.amount > 0;
        return (
          <Stack
            key={t.id}
            direction="row"
            spacing={1.5}
            alignItems="center"
            onClick={onSelect ? () => onSelect(t) : undefined}
            sx={{
              py: 1,
              px: 1,
              mx: -1,
              borderRadius: `${a.radiusControl}px`,
              cursor: onSelect ? "pointer" : "default",
              "&:hover": { backgroundColor: a.surfaceAlt },
            }}
          >
            <Box
              sx={{
                width: 36,
                height: 36,
                flexShrink: 0,
                borderRadius: `${Math.max(a.radiusControl, 6)}px`,
                display: "grid",
                placeItems: "center",
                backgroundColor: isIncome ? a.incomeSoft : a.expenseSoft,
                color: isIncome ? a.income : a.expense,
              }}
            >
              {isIncome ? <SouthWestRounded sx={{ fontSize: 17 }} /> : <NorthEastRounded sx={{ fontSize: 17 }} />}
            </Box>
            <Box sx={{ flex: 1, minWidth: 0 }}>
              <Typography sx={{ fontSize: "0.86rem", fontWeight: 600, color: a.text }} noWrap>
                {t.description || "Senza descrizione"}
              </Typography>
              <Typography sx={{ fontSize: "0.74rem", color: a.textMuted }} noWrap>
                {formatDay(t.date)} · {t.category || "Senza categoria"} · {t.account}
              </Typography>
            </Box>
            <Typography
              sx={{
                fontSize: "0.88rem",
                fontWeight: 700,
                color: isIncome ? a.income : a.text,
                whiteSpace: "nowrap",
                fontVariantNumeric: "tabular-nums",
              }}
            >
              {formatSignedCurrency(t.amount)}
            </Typography>
          </Stack>
        );
      })}
    </Stack>
  );
}
