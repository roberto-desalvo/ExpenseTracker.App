import React, { useState } from "react";
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Snackbar,
} from "@mui/material";

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
  const [snackOpen, setSnackOpen] = useState(false);
  const [snackMessage, setSnackMessage] = useState("");

  const handleConfirm = async () => {
    try {
      const message = await onConfirm();
      if (typeof message === "string" && message) {
        setSnackMessage(message);
        setSnackOpen(true);
      }
      onClose();
    } catch {
      // stay open on error
    }
  };

  return (
    <>
    <Dialog
      open={open}
      onClose={isBusy ? undefined : onClose}
      aria-labelledby="confirm-delete-title"
      aria-describedby="confirm-delete-description"
    >
      <DialogTitle id="confirm-delete-title">{title}</DialogTitle>
      <DialogContent>
        <DialogContentText id="confirm-delete-description">
          {message}
        </DialogContentText>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} color="inherit" disabled={isBusy}>
          {cancelLabel}
        </Button>
        <Button
          onClick={() => void handleConfirm()}
          color="error"
          variant="contained"
          disabled={isBusy}
        >
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
        <Alert
          severity="success"
          variant="filled"
          onClose={() => setSnackOpen(false)}
          sx={{ width: "100%" }}
        >
          {snackMessage}
        </Alert>
      </Snackbar>
    </>
  );
}
