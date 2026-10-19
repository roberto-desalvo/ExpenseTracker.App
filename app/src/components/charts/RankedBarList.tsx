import { useMemo } from "react";
import { Box, Stack, Tooltip, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { formatCurrency, formatPercent } from "../../utilities/format.utilities";
import { ChartEmpty } from "./ChartParts";

export interface RankedItem {
  id: number | string;
  name: string;
  value: number;
}

interface RankedBarListProps {
  items: RankedItem[];
  color: string;
  /** voci mostrate prima di raggruppare il resto in "Altre" */
  maxItems?: number;
  otherLabel?: string;
  emptyMessage?: string;
  minHeight?: number;
}

/**
 * Classifica a barre orizzontali: confronta importi tra voci senza ordine naturale
 * (categorie, conti). Un solo colore: la lunghezza porta il valore, il testo il nome.
 */
export default function RankedBarList({
  items,
  color,
  maxItems = 6,
  otherLabel = "Altre",
  emptyMessage = "Nessun movimento nel periodo",
  minHeight = 260,
}: RankedBarListProps) {
  const a = useTheme().app;

  const { rows, total } = useMemo(() => {
    const positive = items
      .map((item) => ({ ...item, value: Math.abs(item.value) }))
      .filter((item) => item.value > 0)
      .sort((x, y) => y.value - x.value);
    const sum = positive.reduce((acc, item) => acc + item.value, 0);

    if (positive.length <= maxItems) {
      return { rows: positive, total: sum };
    }

    const head = positive.slice(0, maxItems - 1);
    const tail = positive.slice(maxItems - 1);
    return {
      rows: [
        ...head,
        {
          id: "__other__",
          name: `${otherLabel} (${tail.length})`,
          value: tail.reduce((acc, item) => acc + item.value, 0),
        },
      ],
      total: sum,
    };
  }, [items, maxItems, otherLabel]);

  if (rows.length === 0) {
    return <ChartEmpty height={minHeight} message={emptyMessage} />;
  }

  const max = rows[0].value;

  return (
    <Stack spacing={1.6} sx={{ minHeight }}>
      {rows.map((row) => {
        const share = total > 0 ? (row.value / total) * 100 : 0;
        const isOther = row.id === "__other__";
        return (
          <Tooltip
            key={row.id}
            placement="top-start"
            title={`${row.name}: ${formatCurrency(row.value)} · ${formatPercent(share)} del totale`}
          >
            <Box sx={{ cursor: "default" }}>
              <Stack direction="row" alignItems="baseline" spacing={1} sx={{ mb: 0.6 }}>
                <Typography
                  sx={{ flex: 1, minWidth: 0, fontSize: "0.85rem", fontWeight: 500, color: isOther ? a.textSecondary : a.text }}
                  noWrap
                >
                  {row.name}
                </Typography>
                <Typography sx={{ fontSize: "0.85rem", fontWeight: 650, color: a.text }}>
                  {formatCurrency(row.value)}
                </Typography>
                <Typography
                  sx={{ fontSize: "0.75rem", color: a.textMuted, width: 44, textAlign: "right", fontVariantNumeric: "tabular-nums" }}
                >
                  {formatPercent(share, 0)}
                </Typography>
              </Stack>
              <Box sx={{ height: 8, borderRadius: 4, backgroundColor: a.surfaceAlt, overflow: "hidden" }}>
                <Box
                  sx={{
                    height: "100%",
                    width: `${Math.max((row.value / max) * 100, 1.5)}%`,
                    borderRadius: 4,
                    backgroundColor: isOther ? a.chart.other : color,
                    transition: "width .4s ease",
                  }}
                />
              </Box>
            </Box>
          </Tooltip>
        );
      })}
    </Stack>
  );
}
