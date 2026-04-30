import React from "react";
import {
  Box,
  Paper,
  Skeleton,
  Stack,
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
import DataTableFilters from "./DataTableFilters";

export interface DataTableColumn {
  id: string;
  label: string;
  minWidth?: number;
  align?: "left" | "right" | "center" | "inherit" | "justify";
}

export interface DataTableBaseProps<T> {
  title: string;
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
  rowsPerPageOptions?: number[];
  minTableWidth?: number;
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
  rowsPerPageOptions = [10, 25, 100],
  minTableWidth = 760,
}: DataTableBaseProps<T>) {
  const theme = useTheme();
  const c = theme.palette.custom;

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
      {/* Header */}
      <DataTableFilters
        title={title}
        subtitle={subtitle}
        leftContent={headerLeftContent}
        rightContent={headerRightContent}
      />

      {/* Content Area */}
      <TableContainer
        sx={{
          flex: 1,
          minHeight: 0,
          overflow: "auto",
          px: { xs: 1.5, md: 2 },
          py: 1.5,
        }}
      >
        {isLoading ? (
          <Box sx={{ p: 3 }}>
            <Stack spacing={1}>
              {[...Array(5)].map((_, i) => (
                <Skeleton key={i} variant="rectangular" height={40} />
              ))}
            </Stack>
          </Box>
        ) : isEmpty ? (
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
            stickyHeader
            sx={{
              borderCollapse: "separate",
              borderSpacing: "0 10px",
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
                      background: "background.default",
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
              {rows.map((row, index) => (
                <React.Fragment key={index}>{renderRow(row, index)}</React.Fragment>
              ))}
            </TableBody>
          </Table>
        )}
      </TableContainer>

      {/* Pagination */}
      {!isLoading && !isEmpty && (
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
            borderTop: `1px solid ${theme.palette.divider}`,
            ".MuiTablePagination-toolbar": {
              px: { xs: 1.5, md: 2 },
            },
          }}
        />
      )}
    </Paper>
  );
}
