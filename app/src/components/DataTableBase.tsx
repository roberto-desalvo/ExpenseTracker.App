import React from "react";
import {
  Box,
  CircularProgress,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  Typography,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";

export interface DataTableColumn {
  id: string;
  label: string;
  minWidth?: number;
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
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (event: unknown, newPage: number) => void;
  onPageSizeChange: (event: React.ChangeEvent<HTMLInputElement>) => void;
  renderRow: (row: T, index: number) => React.ReactNode;
  headerLeftContent?: React.ReactNode;
  headerRightContent?: React.ReactNode;
  showHeader?: boolean;
  rowsPerPageOptions?: number[];
  minTableWidth?: number;
  tableViewportHeight?: number;
}

export default function DataTableBase<T>({
  title,
  subtitle,
  columns,
  rows,
  isLoading = false,
  isEmpty = false,
  emptyMessage = "Nessun dato trovato",
  emptySubtext = "",
  page,
  pageSize,
  totalCount,
  onPageChange,
  onPageSizeChange,
  renderRow,
  headerLeftContent,
  headerRightContent,
  showHeader = false,
  rowsPerPageOptions = [10, 25, 100],
  minTableWidth = 700,
  tableViewportHeight = 576,
}: DataTableBaseProps<T>) {
  void title;
  void subtitle;
  void headerLeftContent;
  void headerRightContent;
  void showHeader;
  const theme = useTheme();
  const c = theme.palette.custom;

  return (
    <Paper
      sx={{
        height: "auto",
        width: "100%",
        overflow: "visible",
        background: "transparent",
        display: "flex",
        flexDirection: "column",
        borderRadius: 4,
        border: `1px solid ${theme.palette.divider}`,
        position: "relative",
      }}
    >
      {/* Loading overlay — keeps table layout stable */}
      {isLoading && (
        <Box
          sx={{
            position: "absolute",
            inset: 0,
            zIndex: 2,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            borderRadius: 4,
            backgroundColor:
              theme.palette.mode === "dark"
                ? "rgba(15,23,42,0.55)"
                : "rgba(255,255,255,0.65)",
            backdropFilter: "blur(2px)",
          }}
        >
          <CircularProgress size={32} thickness={3.5} />
        </Box>
      )}

      {/* Content Area */}
      <TableContainer
        sx={{
          overflowX: "auto",
          overflowY: tableViewportHeight ? "auto" : "visible",
          scrollbarGutter: "stable",
          height: tableViewportHeight,
          px: { xs: 1.25, md: 1.75 },
          py: { xs: 1.25, md: 1.5 },
          "& .MuiTableCell-head": tableViewportHeight
            ? {
                position: "sticky",
                top: 0,
                zIndex: 3,
              }
            : undefined,
        }}
      >
        {isEmpty && !isLoading ? (
          <Box
            sx={{
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              height: "100%",
              minHeight: 300,
              flexDirection: "column",
              gap: 2,
            }}
          >
            <Typography variant="h6" sx={{ color: "text.secondary" }}>
              {emptyMessage}
            </Typography>
            {emptySubtext && (
              <Typography variant="body2" sx={{ color: "text.secondary" }}>
                {emptySubtext}
              </Typography>
            )}
          </Box>
        ) : (
          <Table
            sx={{
              borderCollapse: "separate",
              borderSpacing: "0 6px",
              minWidth: minTableWidth,
            }}
          >
            <TableHead>
              <TableRow>
                {columns.map((column) => (
                  <TableCell
                    key={column.id}
                    align={column.align}
                    sx={{
                      width: column.minWidth,
                      backgroundColor: theme.palette.background.paper,
                      color: c.tableHeaderText,
                      textTransform: "uppercase",
                      fontSize: "0.66rem",
                      fontWeight: 700,
                      letterSpacing: "0.1em",
                      borderBottom: "none",
                      px: 1.5,
                      py: 0.5,
                    }}
                  >
                    {column.label}
                  </TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading
                ? [...Array(pageSize)].map((_, i) => (
                    <TableRow
                      key={i}
                      sx={{
                        "& td:first-of-type": {
                          borderTopLeftRadius: 8,
                          borderBottomLeftRadius: 8,
                        },
                        "& td:last-of-type": {
                          borderTopRightRadius: 8,
                          borderBottomRightRadius: 8,
                        },
                      }}
                    >
                      {columns.map((column) => (
                        <TableCell
                          key={column.id}
                          sx={{
                            borderBottom: "none",
                            backgroundColor: c.rowBackground,
                            py: 1,
                          }}
                        >
                          <Box
                            sx={{
                              height: 14,
                              width: "60%",
                              borderRadius: 1,
                              backgroundColor: theme.palette.action.hover,
                            }}
                          />
                        </TableCell>
                      ))}
                    </TableRow>
                  ))
                : rows.map((row, index) => (
                    <React.Fragment key={index}>{renderRow(row, index)}</React.Fragment>
                  ))}
            </TableBody>
          </Table>
        )}
      </TableContainer>

      {/* Pagination */}
      {!isEmpty && (
        <TablePagination
          rowsPerPageOptions={rowsPerPageOptions}
          component="div"
          labelRowsPerPage="Elementi per pagina"
          labelDisplayedRows={({ from, to, count }) =>
            `${from}-${to} di ${count !== -1 ? count : `più di ${to}`}`
          }
          count={totalCount}
          rowsPerPage={pageSize}
          page={page}
          onPageChange={onPageChange}
          onRowsPerPageChange={onPageSizeChange}
          sx={{
            position: "relative",
            zIndex: 3,
            backgroundColor: theme.palette.background.paper,
            borderTop: `1px solid ${theme.palette.divider}`,
            ".MuiTablePagination-toolbar": {
              px: { xs: 1.25, md: 1.75 },
            },
          }}
        />
      )}
    </Paper>
  );
}
