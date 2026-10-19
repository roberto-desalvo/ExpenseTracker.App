import { ReactNode } from "react";
import { Box, Stack, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";

interface EmptyStateProps {
  icon?: ReactNode;
  title: string;
  text?: string;
  action?: ReactNode;
  minHeight?: number;
}

export default function EmptyState({ icon, title, text, action, minHeight = 220 }: EmptyStateProps) {
  const a = useTheme().app;

  return (
    <Stack alignItems="center" justifyContent="center" spacing={1.25} sx={{ minHeight, textAlign: "center", px: 2 }}>
      {icon && (
        <Box
          sx={{
            width: 48,
            height: 48,
            borderRadius: `${a.radiusControl + 4}px`,
            display: "grid",
            placeItems: "center",
            backgroundColor: a.surfaceAlt,
            color: a.textSecondary,
          }}
        >
          {icon}
        </Box>
      )}
      <Typography sx={{ fontWeight: 600, color: a.text }}>{title}</Typography>
      {text && <Typography sx={{ fontSize: "0.85rem", color: a.textSecondary, maxWidth: 360 }}>{text}</Typography>}
      {action}
    </Stack>
  );
}
