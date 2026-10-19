import React, { useState } from "react";
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  IconButton,
  Snackbar,
  Stack,
  Typography,
  useMediaQuery,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { CloseRounded } from "@mui/icons-material";

export interface AppModalProps {
  open: boolean;
  onClose: () => void;
  title: string;
  subtitle?: string;
  onSubmit: (e: React.FormEvent) => string | void | Promise<string | void>;
  isBusy?: boolean;
  submitLabel?: string;
  cancelLabel?: string;
  submitVariant?: "primary" | "error" | "warning";
  maxWidth?: number;
  children: React.ReactNode;
}

/** Modale con form: intestazione, campi, azioni. A schermo intero su mobile. */
export default function AppModal({
  open,
  onClose,
  title,
  subtitle,
  onSubmit,
  isBusy = false,
  submitLabel = "Salva",
  cancelLabel = "Annulla",
  submitVariant = "primary",
  maxWidth = 480,
  children,
}: AppModalProps) {
  const theme = useTheme();
  const a = theme.app;
  const fullScreen = useMediaQuery(theme.breakpoints.down("sm"));
  const [snackOpen, setSnackOpen] = useState(false);
  const [snackMessage, setSnackMessage] = useState("");

  const handleFormSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const message = await onSubmit(e);
      if (typeof message === "string" && message) {
        setSnackMessage(message);
        setSnackOpen(true);
      }
      onClose();
    } catch {
      // resta aperta in caso di errore: lo mostra il layer API
    }
  };

  return (
    <>
      <Dialog
        open={open}
        onClose={isBusy ? undefined : onClose}
        aria-labelledby="app-modal-title"
        fullScreen={fullScreen}
        PaperProps={{ sx: { width: "100%", maxWidth: fullScreen ? "100%" : maxWidth, borderRadius: fullScreen ? 0 : undefined } }}
      >
        <form onSubmit={(e) => void handleFormSubmit(e)} style={{ display: "contents" }}>
          <Stack direction="row" alignItems="flex-start" spacing={2} sx={{ px: 3, pt: 2.75, pb: 1 }}>
            <Box sx={{ flex: 1 }}>
              <Typography id="app-modal-title" component="h2" sx={{ fontWeight: 700, fontSize: "1.1rem", letterSpacing: "-0.01em" }}>
                {title}
              </Typography>
              {subtitle && <Typography sx={{ fontSize: "0.84rem", color: a.textSecondary, mt: 0.25 }}>{subtitle}</Typography>}
            </Box>
            <IconButton onClick={onClose} disabled={isBusy} aria-label="Chiudi" size="small" sx={{ color: a.textMuted, mt: -0.5, mr: -1 }}>
              <CloseRounded fontSize="small" />
            </IconButton>
          </Stack>
          <DialogContent sx={{ px: 3, pt: "12px !important" }}>
            <Stack spacing={2.25}>{children}</Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, py: 2, gap: 1, borderTop: `1px solid ${a.border}`, mt: 1 }}>
            <Button variant="outlined" onClick={onClose} disabled={isBusy}>
              {cancelLabel}
            </Button>
            <Button
              variant="contained"
              type="submit"
              disabled={isBusy}
              color={submitVariant === "primary" ? "primary" : submitVariant}
              sx={{ minWidth: 120 }}
            >
              {isBusy ? "Attendere..." : submitLabel}
            </Button>
          </DialogActions>
        </form>
      </Dialog>
      <Snackbar
        open={snackOpen}
        autoHideDuration={4000}
        onClose={() => setSnackOpen(false)}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      >
        <Alert severity="success" variant="filled" onClose={() => setSnackOpen(false)} sx={{ width: "100%" }}>
          {snackMessage}
        </Alert>
      </Snackbar>
    </>
  );
}
