import { Box, IconButton, Stack, Tooltip, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { TrendingDownRounded, TrendingUpRounded, VisibilityOffRounded, VisibilityRounded } from "@mui/icons-material";
import NetWorthAreaChart, { AmountPoint } from "../charts/NetWorthAreaChart";
import { formatCurrency, formatPercent, formatSignedCurrency } from "../../utilities/format.utilities";

interface HeroBalanceCardProps {
  balance: number;
  series: AmountPoint[];
  hidden: boolean;
  onToggleHidden: () => void;
  asOfText: string;
}

/** Card principale: patrimonio attuale, variazione su 12 mesi e sparkline. */
export default function HeroBalanceCard({ balance, series, hidden, onToggleHidden, asOfText }: HeroBalanceCardProps) {
  const a = useTheme().app;
  const h = a.hero;

  const first = series[0]?.amount;
  const delta = first !== undefined ? balance - first : null;
  const deltaPct = first !== undefined && first !== 0 ? (delta! / Math.abs(first)) * 100 : null;
  const up = (delta ?? 0) >= 0;

  return (
    <Box
      sx={{
        position: "relative",
        overflow: "hidden",
        background: h.background,
        color: h.text,
        border: `1px solid ${h.border}`,
        borderRadius: `${a.radiusCard}px`,
        p: { xs: 2.5, md: 3 },
        minHeight: 260,
        display: "flex",
        flexDirection: "column",
        animation: "fadeUp .35s ease-out both",
      }}
    >
      {/* decorazione: cerchi sfumati, solo per lo stile Aurora */}
      {a.presetId === "aurora" && (
        <Box
          aria-hidden
          sx={{
            position: "absolute",
            right: -80,
            top: -120,
            width: 320,
            height: 320,
            borderRadius: "50%",
            background: "radial-gradient(circle, rgba(255,255,255,0.22) 0%, rgba(255,255,255,0) 70%)",
          }}
        />
      )}

      <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ position: "relative" }}>
        <Typography sx={{ fontSize: "0.82rem", fontWeight: 600, color: h.subtle, letterSpacing: "0.02em" }}>
          Patrimonio netto
        </Typography>
        <Tooltip title={hidden ? "Mostra importi" : "Nascondi importi"}>
          <IconButton
            size="small"
            onClick={onToggleHidden}
            aria-label={hidden ? "Mostra importi" : "Nascondi importi"}
            sx={{ color: h.subtle, "&:hover": { backgroundColor: "rgba(255,255,255,0.1)" } }}
          >
            {hidden ? <VisibilityOffRounded fontSize="small" /> : <VisibilityRounded fontSize="small" />}
          </IconButton>
        </Tooltip>
      </Stack>

      <Typography
        sx={{
          position: "relative",
          fontFamily: a.headingFontFamily,
          fontSize: { xs: "2.1rem", md: "2.6rem" },
          fontWeight: 750,
          letterSpacing: "-0.03em",
          lineHeight: 1.1,
          mt: 1,
        }}
      >
        {hidden ? "•••••• €" : formatCurrency(balance)}
      </Typography>

      {delta !== null && (
        <Stack direction="row" alignItems="center" spacing={1} sx={{ mt: 1.25, position: "relative" }}>
          <Stack
            direction="row"
            alignItems="center"
            spacing={0.5}
            sx={{
              px: 1,
              py: 0.25,
              borderRadius: 999,
              backgroundColor: "rgba(255,255,255,0.14)",
              fontSize: "0.78rem",
              fontWeight: 650,
            }}
          >
            {up ? <TrendingUpRounded sx={{ fontSize: 16 }} /> : <TrendingDownRounded sx={{ fontSize: 16 }} />}
            <span>
              {hidden ? "•••" : formatSignedCurrency(delta)}
              {deltaPct !== null && ` (${up ? "+" : "−"}${formatPercent(Math.abs(deltaPct))})`}
            </span>
          </Stack>
          <Typography sx={{ fontSize: "0.78rem", color: h.subtle }}>negli ultimi 12 mesi</Typography>
        </Stack>
      )}

      <Box sx={{ flex: 1, minHeight: 24 }} />

      <Box sx={{ mx: -1, mb: -1, position: "relative" }}>
        <NetWorthAreaChart points={series} variant="spark" height={96} color={h.spark} />
      </Box>

      <Typography sx={{ fontSize: "0.7rem", color: h.subtle, mt: 1, position: "relative" }}>
        Media mensile dei saldi · aggiornato {asOfText}
      </Typography>
    </Box>
  );
}
