import { Box, Button, Typography } from "@mui/material";
import ErrorOutlineIcon from "@mui/icons-material/ErrorOutline";
import { useTheme } from "@mui/material/styles";
import { useMsal } from "@azure/msal-react";
import { loginRequest } from "../config/authConfig";
import { MsalAuthenticationResult } from "@azure/msal-react";

export default function AuthErrorPage({ error }: MsalAuthenticationResult) {
  const { instance } = useMsal();
  const theme = useTheme();

  const handleRetry = () => {
    instance.loginRedirect(loginRequest);
  };

  return (
    <Box
      sx={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        flexGrow: 1,
        minHeight: "100vh",
        gap: 2,
        px: 3,
      }}
    >
      <ErrorOutlineIcon sx={{ fontSize: 56, color: theme.palette.error.main }} />
      <Typography variant="h6" sx={{ fontWeight: 700, color: "text.primary", textAlign: "center" }}>
        Accesso non riuscito
      </Typography>
      <Typography variant="body2" sx={{ color: "text.secondary", textAlign: "center", maxWidth: 360 }}>
        {error?.message ?? "Si è verificato un errore durante l'autenticazione."}
      </Typography>
      <Button
        variant="contained"
        onClick={handleRetry}
        sx={{ mt: 1, borderRadius: "10px", textTransform: "none", fontWeight: 600 }}
      >
        Riprova
      </Button>
    </Box>
  );
}
