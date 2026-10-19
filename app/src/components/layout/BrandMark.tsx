import { Box, Stack, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";

interface BrandMarkProps {
  compact?: boolean;
  onClick?: () => void;
}

export default function BrandMark({ compact = false, onClick }: BrandMarkProps) {
  const a = useTheme().app;

  return (
    <Stack
      direction="row"
      alignItems="center"
      spacing={1.25}
      onClick={onClick}
      sx={{ cursor: onClick ? "pointer" : "default", userSelect: "none", minWidth: 0 }}
    >
      <Box
        aria-hidden
        sx={{
          width: 34,
          height: 34,
          flexShrink: 0,
          borderRadius: `${Math.max(a.radiusControl, 6)}px`,
          background: a.hero.background,
          border: `1px solid ${a.hero.border}`,
          display: "grid",
          placeItems: "center",
        }}
      >
        {/* tre barre crescenti: il logo */}
        <svg width="18" height="18" viewBox="0 0 18 18" fill="none">
          <rect x="2" y="9" width="3" height="7" rx="1.2" fill={a.hero.text} opacity="0.55" />
          <rect x="7.5" y="5.5" width="3" height="10.5" rx="1.2" fill={a.hero.text} opacity="0.8" />
          <rect x="13" y="2" width="3" height="14" rx="1.2" fill={a.hero.spark} />
        </svg>
      </Box>
      {!compact && (
        <Box sx={{ minWidth: 0 }}>
          <Typography
            sx={{
              fontFamily: a.headingFontFamily,
              fontWeight: 750,
              fontSize: "0.98rem",
              letterSpacing: "-0.015em",
              color: a.text,
              lineHeight: 1.1,
              whiteSpace: "nowrap",
            }}
          >
            Expense Tracker
          </Typography>
          <Typography sx={{ fontSize: "0.7rem", color: a.textMuted, lineHeight: 1.3 }}>
            Finanze personali
          </Typography>
        </Box>
      )}
    </Stack>
  );
}
