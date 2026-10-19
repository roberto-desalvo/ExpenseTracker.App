import { ReactNode } from "react";
import { Box, Stack, SxProps, Theme, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";

interface SurfaceCardProps {
  title?: ReactNode;
  subtitle?: ReactNode;
  /** contenuto a destra dell'intestazione (filtri, toggle, link) */
  action?: ReactNode;
  children?: ReactNode;
  padding?: number;
  sx?: SxProps<Theme>;
  /** rimuove il padding del contenuto (es. tabelle a filo) */
  flush?: boolean;
}

/** Contenitore base di tutte le sezioni: superficie, bordo, raggio e ombra dello stile attivo. */
export default function SurfaceCard({
  title,
  subtitle,
  action,
  children,
  padding = 2.5,
  sx,
  flush = false,
}: SurfaceCardProps) {
  const theme = useTheme();
  const a = theme.app;
  const hasHeader = Boolean(title || subtitle || action);

  return (
    <Box
      sx={[
        {
          backgroundColor: a.surface,
          border: `1px solid ${a.border}`,
          borderRadius: `${a.radiusCard}px`,
          boxShadow: a.cardShadow,
          display: "flex",
          flexDirection: "column",
          minWidth: 0,
          animation: "fadeUp .35s ease-out both",
        },
        ...(Array.isArray(sx) ? sx : [sx]),
      ]}
    >
      {hasHeader && (
        <Stack
          direction="row"
          alignItems="flex-start"
          justifyContent="space-between"
          spacing={1.5}
          useFlexGap
          flexWrap="wrap"
          sx={{ px: padding, pt: padding, pb: flush ? 1.5 : 0 }}
        >
          <Box sx={{ minWidth: 0 }}>
            {title && (
              <Typography
                component="h2"
                sx={{ fontSize: "0.98rem", fontWeight: 650, color: a.text, lineHeight: 1.35 }}
              >
                {title}
              </Typography>
            )}
            {subtitle && (
              <Typography sx={{ fontSize: "0.8rem", color: a.textSecondary, mt: 0.25 }}>
                {subtitle}
              </Typography>
            )}
          </Box>
          {action && <Box sx={{ flexShrink: 0 }}>{action}</Box>}
        </Stack>
      )}
      <Box sx={{ p: flush ? 0 : padding, pt: hasHeader && !flush ? 2 : undefined, flex: 1, minWidth: 0 }}>
        {children}
      </Box>
    </Box>
  );
}
