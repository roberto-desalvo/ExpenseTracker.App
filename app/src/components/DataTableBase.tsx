import React from "react";
import {
  Box,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { InboxOutlined } from "@mui/icons-material";
import EmptyState from "./ui/EmptyState";

export interface DataTableColumn {
  id: string;
  label: string;
  minWidth?: number | string;
  align?: "left" | "right" | "center" | "inherit" | "justify";
}

export interface DataTableBaseProps<T> {
  title?: string;
  subtitle?: React.ReactNode;
  columns: DataTableColumn[];
  rows: T[];
  isLoading?: boolean;
  isEmpty?: boolean;
  emptyMessage?: string;
  emptySubtext?: string;
  emptyAction?: React.ReactNode;
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (event: unknown, newPage: number) => void;
  onPageSizeChange: (event: React.ChangeEvent<HTMLInputElement>) => void;
  renderRow: (row: T, index: number) => React.ReactNode;
  rowsPerPageOptions?: number[];
  minTableWidth?: number;
  /** altezza massima dell'area scrollabile; l'intestazione resta fissa */
  tableViewportHeight?: number;
}

/** Tabella paginata condivisa (movimenti, categorie): righe a filo con separatori sottili. */
export default function DataTableBase<T>({
  columns,
  rows,
  isLoading = false,
  isEmpty = false,
  emptyMessage = "Nessun dato trovato",
  emptySubtext = "",
  emptyAction,
  page,
  pageSize,
  totalCount,
  onPageChange,
  onPageSizeChange,
  renderRow,
  rowsPerPageOptions = [10, 25, 100],
  minTableWidth = 700,
  tableViewportHeight,
}: DataTableBaseProps<T>) {
  const a = useTheme().app;

  return (
    <Box
      sx={{
        width: "100%",
        backgroundColor: a.surface,
        border: `1px solid ${a.border}`,
        borderRadius: `${a.radiusCard}px`,
        boxShadow: a.cardShadow,
        overflow: "hidden",
        display: "flex",
        flexDirection: "column",
      }}
    >
      {isEmpty && !isLoading ? (
        <EmptyState icon={<InboxOutlined />} title={emptyMessage} text={emptySubtext} action={emptyAction} minHeight={320} />
      ) : (
        <TableContainer sx={{ overflowX: "auto", maxHeight: tableViewportHeight }}>
          <Table stickyHeader sx={{ minWidth: minTableWidth }}>
            <TableHead>
              <TableRow>
                {columns.map((column) => (
                  <TableCell
                    key={column.id}
                    align={column.align}
                    sx={{
                      width: column.minWidth,
                      backgroundColor: a.surfaceAlt,
                      borderBottom: `1px solid ${a.border}`,
                      py: 1.25,
                      px: 2,
                      whiteSpace: "nowrap",
                    }}
                  >
                    {column.label}
                  </TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody
              sx={{
                "& .MuiTableRow-root:last-of-type .MuiTableCell-root": { borderBottom: "none" },
                "& .MuiTableCell-body": { px: 2, py: 1.25, borderBottom: `1px solid ${a.border}` },
              }}
            >
              {isLoading
                ? Array.from({ length: Math.min(pageSize, 10) }).map((_, i) => (
                    <TableRow key={i}>
                      {columns.map((column) => (
                        <TableCell key={column.id} align={column.align}>
                          <Skeleton width={column.id === "description" ? "70%" : "55%"} />
                        </TableCell>
                      ))}
                    </TableRow>
                  ))
                : rows.map((row, index) => <React.Fragment key={index}>{renderRow(row, index)}</React.Fragment>)}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {!isEmpty && (
        <TablePagination
          rowsPerPageOptions={rowsPerPageOptions}
          component="div"
          labelRowsPerPage="Per pagina"
          labelDisplayedRows={({ from, to, count }) => `${from}–${to} di ${count !== -1 ? count : `più di ${to}`}`}
          count={totalCount}
          rowsPerPage={pageSize}
          page={page}
          onPageChange={onPageChange}
          onRowsPerPageChange={onPageSizeChange}
          sx={{ borderTop: `1px solid ${a.border}`, ".MuiTablePagination-toolbar": { px: 2 } }}
        />
      )}
    </Box>
  );
}
