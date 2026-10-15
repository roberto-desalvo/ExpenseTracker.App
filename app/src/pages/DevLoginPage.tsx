import { FormEvent, useState } from "react";
import { Box, Button, TextField, Typography } from "@mui/material";
import { DEV_USER_MAX_LENGTH } from "../auth/devAuth";

interface DevLoginPageProps {
  onLogin: (userName: string) => void;
}

/** Login di sviluppo: chiede solo il nome utente (autenticazione Entra ID bypassata). */
export default function DevLoginPage({ onLogin }: DevLoginPageProps) {
  const [userName, setUserName] = useState("");
  const trimmed = userName.trim();

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    if (trimmed) {
      onLogin(trimmed);
    }
  };

  return (
    <Box
      component="form"
      onSubmit={handleSubmit}
      sx={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        flexGrow: 1,
        minHeight: "100vh",
        gap: 2,
        px: 3,
        bgcolor: "background.default",
      }}
    >
      <Typography variant="h6" sx={{ fontWeight: 700, color: "text.primary", textAlign: "center" }}>
        Expense Tracker
      </Typography>
      <Typography variant="body2" sx={{ color: "text.secondary", textAlign: "center", maxWidth: 360 }}>
        Modalità sviluppo: autenticazione disattivata. Indica con quale utente vuoi accedere.
      </Typography>
      <TextField
        label="Nome utente"
        value={userName}
        onChange={(event) => setUserName(event.target.value)}
        autoFocus
        size="small"
        inputProps={{ maxLength: DEV_USER_MAX_LENGTH }}
        sx={{ width: "100%", maxWidth: 320 }}
      />
      <Button
        type="submit"
        variant="contained"
        disabled={!trimmed}
        sx={{ borderRadius: "10px", textTransform: "none", fontWeight: 600 }}
      >
        Accedi
      </Button>
    </Box>
  );
}
