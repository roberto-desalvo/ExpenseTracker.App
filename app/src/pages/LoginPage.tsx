import { useMsal } from "@azure/msal-react";
import { loginRequest } from "../config/authConfig";
import { Box, Button, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import LoginIcon from "@mui/icons-material/Login";

export default function LoginPage() {
  const { instance } = useMsal();
  const theme = useTheme();

  const handleLogin = () => {
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
        gap: 3,
        px: 2,
      }}
    >
      <Typography
        variant="h5"
        sx={{ fontWeight: 700, color: "text.primary", textAlign: "center" }}
      >
        Expense Tracker
      </Typography>
      <Typography
        variant="body2"
        sx={{ color: "text.secondary", textAlign: "center", maxWidth: 320 }}
      >
        Accedi con il tuo account Microsoft per continuare.
      </Typography>
      <Button
        variant="contained"
        size="large"
        startIcon={<LoginIcon />}
        onClick={handleLogin}
        sx={{
          borderRadius: "10px",
          textTransform: "none",
          fontWeight: 600,
          px: 4,
          backgroundColor: theme.palette.primary.main,
        }}
      >
        Accedi con Microsoft
      </Button>
    </Box>
  );
}
