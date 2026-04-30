import { Box, Stack, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import React from "react";

export interface DataTableFiltersProps {
  title: string;
  subtitle?: React.ReactNode;
  leftContent?: React.ReactNode;
  rightContent?: React.ReactNode;
  stacked?: boolean;
}

export default function DataTableFilters({
  title,
  subtitle,
  leftContent,
  rightContent,
  stacked = false,
}: DataTableFiltersProps) {
  const theme = useTheme();

  return (
    <Box
      sx={{
        px: { xs: 2, md: 3 },
        pt: { xs: 2, md: 2 },
        pb: 1.5,
        borderBottom: `1px solid ${theme.palette.divider}`,
      }}
    >
      <Stack
        direction={{ xs: "column", md: stacked ? "column" : "row" }}
        spacing={1.5}
        alignItems={{ xs: "stretch", md: stacked ? "stretch" : "center" }}
        justifyContent="space-between"
      >
        <Stack spacing={0.5}>
          <Typography
            variant="h6"
            sx={{
              color: "text.primary",
              fontWeight: 600,
              letterSpacing: "-0.01em",
              fontSize: "1rem",
            }}
          >
            {title}
          </Typography>
          {subtitle && (
            <Typography variant="caption" sx={{ color: "text.secondary" }}>
              {subtitle}
            </Typography>
          )}
        </Stack>

        {leftContent && <Box>{leftContent}</Box>}

        {rightContent && (
          <Stack
            direction={{ xs: "column", sm: "row" }}
            spacing={1}
            flexWrap="wrap"
            useFlexGap
            sx={{ display: "flex", justifyContent: stacked ? "flex-start" : "flex-end" }}
          >
            {rightContent}
          </Stack>
        )}
      </Stack>
    </Box>
  );
}
