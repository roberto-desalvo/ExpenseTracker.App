import React, { useState } from "react";
import { Alert, Box, Button, Modal, Snackbar, Stack, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";

export interface AppModalProps {
  open: boolean;
  onClose: () => void;
  title: string;
  onSubmit: (e: React.FormEvent) => string | void | Promise<string | void>;
  isBusy?: boolean;
  submitLabel?: string;
  cancelLabel?: string;
  submitVariant?: "primary" | "error" | "warning";
  maxWidth?: number;
  children: React.ReactNode;
}

export default function AppModal({
  open,
  onClose,
  title,
  onSubmit,
  isBusy = false,
  submitLabel = "Salva",
  cancelLabel = "Annulla",
  submitVariant = "primary",
  maxWidth = 460,
  children,
}: AppModalProps) {
  const theme = useTheme();
  const c = theme.palette.custom;
  const isDark = theme.palette.mode === "dark";

  const submitBgColor = {
    primary: c.accentColor,
    error: theme.palette.error.main,
    warning: theme.palette.warning.main,
  }[submitVariant];

  const submitHoverColor = {
    primary: isDark ? "#bef264" : "#4d7c0f",
    error: theme.palette.error.dark,
    warning: theme.palette.warning.dark,
  }[submitVariant];

  const submitTextColor = {
    primary: isDark ? "#0f172a" : "#ffffff",
    error: "#ffffff",
    warning: isDark ? "#0f172a" : "#ffffff",
  }[submitVariant];

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
      // stay open on error; let the page handle error display
    }
  };

  return (
    <>
    <Modal
      open={open}
      onClose={isBusy ? undefined : onClose}
      aria-labelledby="app-modal-title"
      slotProps={{
        backdrop: {
          sx: {
            backgroundColor: isDark
              ? "rgba(2, 6, 23, 0.7)"
              : "rgba(15, 23, 42, 0.35)",
            backdropFilter: "blur(2px)",
          },
        },
      }}
    >
      <Box
        sx={{
          position: "absolute",
          top: "50%",
          left: "50%",
          transform: "translate(-50%, -50%)",
          width: { xs: "calc(100% - 24px)", sm: maxWidth },
          maxWidth,
          bgcolor: c.drawerBackground,
          border: `1px solid ${c.drawerBorder}`,
          borderRadius: "14px",
          boxShadow: isDark
            ? "0 16px 40px rgba(0,0,0,0.45)"
            : "0 16px 40px rgba(15,23,42,0.16)",
          p: { xs: 2, sm: 3 },
        }}
      >
          <form onSubmit={(e) => void handleFormSubmit(e)}>
          <Stack direction="column" spacing={2}>
            <Typography
              id="app-modal-title"
              variant="h6"
              component="h2"
              sx={{ fontWeight: 600 }}
            >
              {title}
            </Typography>

            {children}

            <Stack
              direction="row"
              spacing={1.25}
              justifyContent="flex-end"
              sx={{ pt: 1 }}
            >
              <Button
                variant="outlined"
                onClick={onClose}
                disabled={isBusy}
                sx={{
                  minWidth: 120,
                  borderColor: c.filterBorder,
                  color: theme.palette.text.secondary,
                  "&:hover": {
                    borderColor: c.drawerBorder,
                    backgroundColor: c.filterBackground,
                  },
                }}
              >
                {cancelLabel}
              </Button>
              <Button
                variant="contained"
                type="submit"
                disabled={isBusy}
                sx={{
                  minWidth: 120,
                  backgroundColor: submitBgColor,
                  color: submitTextColor,
                  "&:hover": { backgroundColor: submitHoverColor },
                }}
              >
                {isBusy ? "Attendere..." : submitLabel}
              </Button>
            </Stack>
          </Stack>
        </form>
      </Box>
    </Modal>
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
