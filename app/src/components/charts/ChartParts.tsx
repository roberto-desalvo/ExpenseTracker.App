import { ReactNode } from "react";
import {
  Box,
  ButtonBase,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
  Typography,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { BarChartRounded, TableRowsRounded } from "@mui/icons-material";

/** Contenitore dei tooltip dei grafici. */
export function ChartTooltipCard({ title, children }: { title: ReactNode; children: ReactNode }) {
  const a = useTheme().app;

  return (
    <Box
      sx={{
        minWidth: 180,
        px: 1.5,
        py: 1.25,
        backgroundColor: a.surface,
        border: `1px solid ${a.borderStrong}`,
        borderRadius: `${a.radiusControl + 2}px`,
        boxShadow: a.mode === "dark" ? "0 12px 32px rgba(0,0,0,0.5)" : "0 12px 28px rgba(15,23,42,0.14)",
        pointerEvents: "none",
      }}
    >
      <Typography sx={{ fontSize: "0.75rem", fontWeight: 600, color: a.textSecondary, mb: 0.75 }}>{title}</Typography>
      <Stack spacing={0.5}>{children}</Stack>
    </Box>
  );
}

export function ChartTooltipRow({
  color,
  label,
  value,
  emphasis = false,
}: {
  color?: string;
  label: string;
  value: string;
  emphasis?: boolean;
}) {
  const a = useTheme().app;

  return (
    <Stack direction="row" alignItems="center" spacing={1}>
      {color ? (
        <Box sx={{ width: 8, height: 8, borderRadius: "2px", backgroundColor: color, flexShrink: 0 }} />
      ) : (
        <Box sx={{ width: 8, flexShrink: 0 }} />
      )}
      <Typography sx={{ fontSize: "0.8rem", color: a.textSecondary, flex: 1 }}>{label}</Typography>
      <Typography
        sx={{
          fontSize: "0.8rem",
          fontWeight: emphasis ? 700 : 600,
          color: a.text,
          fontVariantNumeric: "tabular-nums",
          ml: 2,
        }}
      >
        {value}
      </Typography>
    </Stack>
  );
}

export interface LegendEntry {
  key: string;
  label: string;
  color: string;
  hidden?: boolean;
  /** forma del marcatore: quadrato per barre/aree, linea per serie a linea */
  shape?: "square" | "line";
}

/** Legenda sopra il grafico; se `onToggle` è presente le voci mostrano/nascondono la serie. */
export function ChartLegend({ entries, onToggle }: { entries: LegendEntry[]; onToggle?: (key: string) => void }) {
  const a = useTheme().app;

  return (
    <Stack direction="row" spacing={0.5} useFlexGap flexWrap="wrap" sx={{ mb: 1.5 }}>
      {entries.map((entry) => {
        const content = (
          <Stack direction="row" spacing={0.75} alignItems="center">
            <Box
              sx={{
                width: entry.shape === "line" ? 14 : 9,
                height: entry.shape === "line" ? 2.5 : 9,
                borderRadius: entry.shape === "line" ? 2 : "2px",
                backgroundColor: entry.hidden ? "transparent" : entry.color,
                border: entry.hidden ? `1.5px solid ${entry.color}` : "none",
              }}
            />
            <Typography
              sx={{
                fontSize: "0.78rem",
                color: entry.hidden ? a.textMuted : a.textSecondary,
                textDecoration: entry.hidden ? "line-through" : "none",
                whiteSpace: "nowrap",
              }}
            >
              {entry.label}
            </Typography>
          </Stack>
        );

        if (!onToggle) {
          return (
            <Box key={entry.key} sx={{ px: 0.75, py: 0.25 }}>
              {content}
            </Box>
          );
        }

        return (
          <ButtonBase
            key={entry.key}
            onClick={() => onToggle(entry.key)}
            aria-pressed={!entry.hidden}
            sx={{
              px: 0.75,
              py: 0.25,
              borderRadius: `${a.radiusControl}px`,
              "&:hover": { backgroundColor: a.surfaceAlt },
            }}
          >
            {content}
          </ButtonBase>
        );
      })}
    </Stack>
  );
}

export type ChartView = "chart" | "table";

/** Switch grafico/tabella: ogni grafico ha una vista tabellare equivalente. */
export function ChartViewToggle({ value, onChange }: { value: ChartView; onChange: (view: ChartView) => void }) {
  return (
    <ToggleButtonGroup
      size="small"
      exclusive
      value={value}
      onChange={(_e, next: ChartView | null) => next && onChange(next)}
      aria-label="Vista"
    >
      <ToggleButton value="chart" aria-label="Grafico">
        <Tooltip title="Grafico">
          <BarChartRounded sx={{ fontSize: 18 }} />
        </Tooltip>
      </ToggleButton>
      <ToggleButton value="table" aria-label="Tabella">
        <Tooltip title="Tabella">
          <TableRowsRounded sx={{ fontSize: 18 }} />
        </Tooltip>
      </ToggleButton>
    </ToggleButtonGroup>
  );
}

export interface ChartTableColumn {
  key: string;
  label: string;
  align?: "left" | "right";
}

export function ChartDataTable({
  columns,
  rows,
  height,
}: {
  columns: ChartTableColumn[];
  rows: Record<string, ReactNode>[];
  height: number;
}) {
  const a = useTheme().app;

  return (
    <TableContainer sx={{ maxHeight: height, minHeight: Math.min(height, 200) }}>
      <Table size="small" stickyHeader>
        <TableHead>
          <TableRow>
            {columns.map((column) => (
              <TableCell key={column.key} align={column.align} sx={{ backgroundColor: a.surface }}>
                {column.label}
              </TableCell>
            ))}
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.map((row, index) => (
            <TableRow key={index} hover>
              {columns.map((column) => (
                <TableCell
                  key={column.key}
                  align={column.align}
                  sx={{ fontSize: "0.82rem", fontVariantNumeric: "tabular-nums", color: a.text }}
                >
                  {row[column.key]}
                </TableCell>
              ))}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}

export function ChartEmpty({ height, message = "Nessun dato per il periodo selezionato" }: { height: number; message?: string }) {
  const a = useTheme().app;

  return (
    <Box
      sx={{
        height,
        display: "grid",
        placeItems: "center",
        color: a.textMuted,
        fontSize: "0.85rem",
        border: `1px dashed ${a.border}`,
        borderRadius: `${a.radiusControl}px`,
      }}
    >
      {message}
    </Box>
  );
}
