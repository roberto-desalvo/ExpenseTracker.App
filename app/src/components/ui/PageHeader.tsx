import { ReactNode } from "react";
import { Box, Stack, Typography } from "@mui/material";

interface PageHeaderProps {
  title: ReactNode;
  subtitle?: ReactNode;
  eyebrow?: ReactNode;
  actions?: ReactNode;
}

export default function PageHeader({ title, subtitle, eyebrow, actions }: PageHeaderProps) {
  return (
    <Stack
      direction={{ xs: "column", md: "row" }}
      alignItems={{ xs: "stretch", md: "flex-end" }}
      justifyContent="space-between"
      spacing={2}
    >
      <Box sx={{ minWidth: 0 }}>
        {eyebrow && (
          <Typography variant="overline" sx={{ color: "text.secondary", display: "block", mb: 0.25 }}>
            {eyebrow}
          </Typography>
        )}
        <Typography variant="h5" component="h1" sx={{ fontSize: { xs: "1.45rem", md: "1.7rem" } }}>
          {title}
        </Typography>
        {subtitle && (
          <Typography sx={{ color: "text.secondary", fontSize: "0.9rem", mt: 0.5 }}>{subtitle}</Typography>
        )}
      </Box>
      {actions && (
        <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap" alignItems="center">
          {actions}
        </Stack>
      )}
    </Stack>
  );
}
