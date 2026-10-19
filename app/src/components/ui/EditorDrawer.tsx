import React from "react";
import { Box, Button, Drawer, IconButton, Stack, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { CloseRounded } from "@mui/icons-material";

interface EditorDrawerProps {
  open: boolean;
  onClose: () => void;
  title: string;
  subtitle?: string;
  onSubmit: (e: React.FormEvent) => void;
  isBusy?: boolean;
  submitLabel?: string;
  submitDisabled?: boolean;
  /** azione secondaria a sinistra del footer (es. elimina) */
  secondaryAction?: React.ReactNode;
  children: React.ReactNode;
}

/** Pannello laterale per creare/modificare un elemento senza perdere di vista l'elenco. */
export default function EditorDrawer({
  open,
  onClose,
  title,
  subtitle,
  onSubmit,
  isBusy = false,
  submitLabel = "Salva",
  submitDisabled = false,
  secondaryAction,
  children,
}: EditorDrawerProps) {
  const a = useTheme().app;

  return (
    <Drawer
      anchor="right"
      open={open}
      onClose={isBusy ? undefined : onClose}
      PaperProps={{
        sx: {
          width: { xs: "100%", sm: 440 },
          backgroundColor: a.surface,
          borderLeft: `1px solid ${a.border}`,
        },
      }}
    >
      <Box
        component="form"
        onSubmit={(e: React.FormEvent) => {
          e.preventDefault();
          onSubmit(e);
        }}
        sx={{ display: "flex", flexDirection: "column", height: "100%" }}
      >
        <Stack direction="row" alignItems="flex-start" spacing={2} sx={{ px: 3, py: 2.5, borderBottom: `1px solid ${a.border}` }}>
          <Box sx={{ flex: 1 }}>
            <Typography component="h2" sx={{ fontWeight: 700, fontSize: "1.1rem" }}>
              {title}
            </Typography>
            {subtitle && <Typography sx={{ fontSize: "0.84rem", color: a.textSecondary, mt: 0.25 }}>{subtitle}</Typography>}
          </Box>
          <IconButton onClick={onClose} disabled={isBusy} aria-label="Chiudi" size="small" sx={{ color: a.textMuted }}>
            <CloseRounded fontSize="small" />
          </IconButton>
        </Stack>

        <Stack spacing={2.5} sx={{ px: 3, py: 3, flex: 1, overflowY: "auto" }}>
          {children}
        </Stack>

        <Stack
          direction="row"
          spacing={1}
          alignItems="center"
          sx={{ px: 3, py: 2, borderTop: `1px solid ${a.border}`, backgroundColor: a.surfaceAlt }}
        >
          {secondaryAction}
          <Box sx={{ flex: 1 }} />
          <Button variant="outlined" onClick={onClose} disabled={isBusy}>
            Annulla
          </Button>
          <Button type="submit" variant="contained" disabled={isBusy || submitDisabled} sx={{ minWidth: 110 }}>
            {isBusy ? "Salvataggio..." : submitLabel}
          </Button>
        </Stack>
      </Box>
    </Drawer>
  );
}
