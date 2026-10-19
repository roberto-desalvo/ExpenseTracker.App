import { FormEvent, useState } from "react";
import { Alert, Box, Button, Stack, TextField, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { DEV_USER_MAX_LENGTH } from "../auth/devAuth";
import BrandMark from "../components/layout/BrandMark";

interface DevLoginPageProps {
  onLogin: (userName: string) => void;
}

/** Login di sviluppo: chiede solo il nome utente (autenticazione Entra ID bypassata). */
export default function DevLoginPage({ onLogin }: DevLoginPageProps) {
  const a = useTheme().app;
  const [userName, setUserName] = useState("");
  const trimmed = userName.trim();

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    if (trimmed) {
      onLogin(trimmed);
    }
  };

  return (
    <Box sx={{ minHeight: "100vh", display: "grid", placeItems: "center", px: 2, bgcolor: a.background }}>
      <Box
        component="form"
        onSubmit={handleSubmit}
        sx={{
          width: "100%",
          maxWidth: 400,
          p: { xs: 3, sm: 4 },
          backgroundColor: a.surface,
          border: `1px solid ${a.border}`,
          borderRadius: `${a.radiusCard}px`,
          boxShadow: a.cardShadow,
        }}
      >
        <Stack spacing={2.5}>
          <BrandMark />
          <Box>
            <Typography variant="h6" component="h1">
              Accedi
            </Typography>
            <Typography sx={{ fontSize: "0.86rem", color: a.textSecondary, mt: 0.5 }}>
              Indica con quale utente vuoi entrare.
            </Typography>
          </Box>
          <Alert severity="info" variant="outlined" sx={{ fontSize: "0.8rem" }}>
            Modalità sviluppo: autenticazione Entra ID disattivata.
          </Alert>
          <TextField
            label="Nome utente"
            value={userName}
            onChange={(event) => setUserName(event.target.value)}
            autoFocus
            inputProps={{ maxLength: DEV_USER_MAX_LENGTH }}
          />
          <Button type="submit" variant="contained" size="large" disabled={!trimmed}>
            Accedi
          </Button>
        </Stack>
      </Box>
    </Box>
  );
}
