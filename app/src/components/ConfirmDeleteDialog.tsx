import React, { useState } from "react";
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  Snackbar,
  Stack,
  Typography,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { DeleteOutlineRounded } from "@mui/icons-material";

export interface ConfirmDeleteDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: () => string | void | Promise<string | void>;
  message: React.ReactNode;
  title?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  isBusy?: boolean;
}

export default function ConfirmDeleteDialog({
  open,
  onClose,
  onConfirm,
  message,
  title = "Conferma eliminazione",
  confirmLabel = "Elimina",
  cancelLabel = "Annulla",
  isBusy = false,
}: ConfirmDeleteDialogProps) {
  const a = useTheme().app;
  const [snackOpen, setSnackOpen] = useState(false);
  const [snackMessage, setSnackMessage] = useState("");

  const handleConfirm = async () => {
    try {
      const result = await onConfirm();
      if (typeof result === "string" && result) {
        setSnackMessage(result);
        setSnackOpen(true);
      }
      onClose();
    } catch {
      // resta aperta in caso di errore
    }
  };

  return (
    <>
      <Dialog
        open={open}
        onClose={isBusy ? undefined : onClose}
        aria-labelledby="confirm-delete-title"
        aria-describedby="confirm-delete-description"
        maxWidth="xs"
        fullWidth
      >
        <DialogContent sx={{ pt: 3 }}>
          <Stack direction="row" spacing={2} alignItems="flex-start">
            <Box
              sx={{
                width: 40,
                height: 40,
                flexShrink: 0,
                borderRadius: `${Math.max(a.radiusControl, 8)}px`,
                display: "grid",
                placeItems: "center",
                backgroundColor: a.expenseSoft,
                color: a.expense,
              }}
            >
              <DeleteOutlineRounded />
            </Box>
            <Box>
              <Typography id="confirm-delete-title" sx={{ fontWeight: 650, fontSize: "1.02rem", mb: 0.75 }}>
                {title}
              </Typography>
              <Typography id="confirm-delete-description" component="div" sx={{ fontSize: "0.88rem", color: a.textSecondary }}>
                {message}
              </Typography>
            </Box>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5, gap: 1 }}>
          <Button variant="outlined" onClick={onClose} disabled={isBusy}>
            {cancelLabel}
          </Button>
          <Button onClick={() => void handleConfirm()} color="error" variant="contained" disabled={isBusy}>
            {isBusy ? "Eliminazione..." : confirmLabel}
          </Button>
        </DialogActions>
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
