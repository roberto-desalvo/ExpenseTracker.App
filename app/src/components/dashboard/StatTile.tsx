import { ReactNode } from "react";
import { Box, Stack, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";

export type StatTone = "income" | "expense" | "neutral" | "accent";

interface StatTileProps {
  label: string;
  value: string;
  icon: ReactNode;
  tone?: StatTone;
  footnote?: ReactNode;
  /** 0-100: barra di avanzamento opzionale sotto il valore */
  progress?: number;
  progressLabel?: string;
}

/** Tile con un solo numero: quando la storia è un valore, il numero è il grafico. */
export default function StatTile({ label, value, icon, tone = "neutral", footnote, progress, progressLabel }: StatTileProps) {
  const a = useTheme().app;

  const toneColor = { income: a.income, expense: a.expense, neutral: a.textSecondary, accent: a.primary }[tone];
  const toneSoft = { income: a.incomeSoft, expense: a.expenseSoft, neutral: a.surfaceAlt, accent: a.primarySoft }[tone];

  return (
    <Box
      sx={{
        height: "100%",
        backgroundColor: a.surface,
        border: `1px solid ${a.border}`,
        borderRadius: `${a.radiusCard}px`,
        boxShadow: a.cardShadow,
        p: { xs: 1.75, sm: 2.25 },
        display: "flex",
        flexDirection: "column",
        gap: 1.25,
        animation: "fadeUp .35s ease-out both",
      }}
    >
      <Stack direction="row" alignItems="center" spacing={1.25}>
        <Box
          sx={{
            width: 34,
            height: 34,
            borderRadius: `${Math.max(a.radiusControl, 6)}px`,
            display: "grid",
            placeItems: "center",
            backgroundColor: toneSoft,
            color: toneColor,
            "& svg": { fontSize: 19 },
          }}
        >
          {icon}
        </Box>
        <Typography sx={{ fontSize: "0.82rem", fontWeight: 600, color: a.textSecondary }}>{label}</Typography>
      </Stack>

      <Typography
        sx={{
          fontFamily: a.headingFontFamily,
          fontSize: { xs: "1.2rem", sm: "1.45rem", md: "1.6rem" },
          fontWeight: 750,
          letterSpacing: "-0.025em",
          color: a.text,
          lineHeight: 1.15,
        }}
      >
        {value}
      </Typography>

      {progress !== undefined && (
        <Box>
          <Box sx={{ height: 6, borderRadius: 3, backgroundColor: a.surfaceAlt, overflow: "hidden" }}>
            <Box
              sx={{
                width: `${Math.min(Math.max(progress, 0), 100)}%`,
                height: "100%",
                borderRadius: 3,
                backgroundColor: toneColor,
                transition: "width .4s ease",
              }}
            />
          </Box>
          {progressLabel && (
            <Typography sx={{ fontSize: "0.72rem", color: a.textMuted, mt: 0.5 }}>{progressLabel}</Typography>
          )}
        </Box>
      )}

      {footnote && (
        <Box sx={{ mt: "auto", fontSize: "0.78rem", color: a.textSecondary }}>{footnote}</Box>
      )}
    </Box>
  );
}
