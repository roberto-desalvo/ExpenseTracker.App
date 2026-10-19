import { useState } from "react";
import { Alert, Box, Button, CircularProgress, Divider, Stack, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import BrandMark from "../components/layout/BrandMark";

interface LoginPageProps {
  /** Avvia il login Entra ID (redirect MSAL). */
  onEntraLogin: () => void;
  /** Avvia la demo; se assente il pulsante non viene mostrato. */
  onDemoLogin?: () => Promise<void>;
  error?: string | null;
}

/** Pagina iniziale: accesso con l'account aziendale (Entra ID) oppure avvio della demo. */
export default function LoginPage({ onEntraLogin, onDemoLogin, error }: LoginPageProps) {
  const a = useTheme().app;
  const [demoLoading, setDemoLoading] = useState(false);
  const [demoError, setDemoError] = useState<string | null>(null);

  const handleDemo = async () => {
    if (!onDemoLogin) return;
    setDemoError(null);
    setDemoLoading(true);
    try {
      await onDemoLogin();
    } catch (err) {
      setDemoError(err instanceof Error ? err.message : "Avvio della demo non riuscito.");
      setDemoLoading(false);
    }
  };

  const message = demoError ?? error;

  return (
    <Box sx={{ minHeight: "100vh", display: "grid", placeItems: "center", px: 2, bgcolor: a.background }}>
      <Box
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
              Entra con il tuo account Microsoft per gestire i tuoi conti.
            </Typography>
          </Box>
          {message && (
            <Alert severity="error" variant="outlined" sx={{ fontSize: "0.8rem" }}>
              {message}
            </Alert>
          )}
          <Button variant="contained" size="large" onClick={onEntraLogin} disabled={demoLoading}>
            Accedi con Microsoft
          </Button>
          {onDemoLogin && (
            <>
              <Divider sx={{ fontSize: "0.75rem", color: a.textMuted }}>oppure</Divider>
              <Box>
                <Button
                  variant="outlined"
                  size="large"
                  fullWidth
                  onClick={handleDemo}
                  disabled={demoLoading}
                  startIcon={demoLoading ? <CircularProgress size={16} color="inherit" /> : undefined}
                >
                  Prova la demo
                </Button>
                <Typography sx={{ fontSize: "0.76rem", color: a.textMuted, mt: 1, textAlign: "center" }}>
                  Esplora l'app con dati fittizi, senza registrazione.
                </Typography>
              </Box>
            </>
          )}
        </Stack>
      </Box>
    </Box>
  );
}
