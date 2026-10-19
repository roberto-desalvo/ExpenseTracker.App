import { MouseEvent, useState } from "react";
import {
  Avatar,
  Box,
  Button,
  Divider,
  IconButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Tooltip,
  Typography,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import {
  AddRounded,
  CompareArrowsRounded,
  DarkModeOutlined,
  LightModeOutlined,
  LogoutRounded,
  PaletteOutlined,
  ReceiptLongRounded,
} from "@mui/icons-material";
import { useNavigate } from "react-router-dom";
import { useQuickActions } from "../../stores/QuickActionsContext";
import { useAppTheme } from "../../stores/ThemeContext";
import { useAuth } from "../../auth/AuthContext";
import { initialsOf } from "../../utilities/format.utilities";
import { ROUTES } from "../../config/routes";

/** Bottone "Nuovo" con menu: transazione o trasferimento. */
export function NewEntryButton({ iconOnly = false, fullWidth = false }: { iconOnly?: boolean; fullWidth?: boolean }) {
  const { openNewTransaction, openNewTransfer } = useQuickActions();
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);

  const open = (event: MouseEvent<HTMLElement>) => setAnchorEl(event.currentTarget);
  const close = () => setAnchorEl(null);

  return (
    <>
      {iconOnly ? (
        <Tooltip title="Nuovo movimento">
          <IconButton
            onClick={open}
            aria-label="Nuovo movimento"
            sx={{
              bgcolor: "primary.main",
              color: "primary.contrastText",
              "&:hover": { bgcolor: "primary.dark" },
            }}
          >
            <AddRounded />
          </IconButton>
        </Tooltip>
      ) : (
        <Button variant="contained" startIcon={<AddRounded />} onClick={open} fullWidth={fullWidth}>
          Nuovo
        </Button>
      )}
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={close}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
        transformOrigin={{ vertical: "top", horizontal: "right" }}
        slotProps={{ paper: { sx: { minWidth: 240 } } }}
      >
        <MenuItem
          onClick={() => {
            close();
            openNewTransaction();
          }}
        >
          <ListItemIcon>
            <ReceiptLongRounded fontSize="small" />
          </ListItemIcon>
          <ListItemText primary="Transazione" secondary="Entrata o uscita su un conto" />
        </MenuItem>
        <MenuItem
          onClick={() => {
            close();
            openNewTransfer();
          }}
        >
          <ListItemIcon>
            <CompareArrowsRounded fontSize="small" />
          </ListItemIcon>
          <ListItemText primary="Trasferimento" secondary="Spostamento tra due conti" />
        </MenuItem>
      </Menu>
    </>
  );
}

export function ThemeModeButton() {
  const { mode, toggleTheme } = useAppTheme();
  const isDark = mode === "dark";

  return (
    <Tooltip title={isDark ? "Passa al tema chiaro" : "Passa al tema scuro"}>
      <IconButton onClick={toggleTheme} aria-label="Cambia tema" sx={{ color: "text.secondary" }}>
        {isDark ? <LightModeOutlined fontSize="small" /> : <DarkModeOutlined fontSize="small" />}
      </IconButton>
    </Tooltip>
  );
}

export function UserMenuButton() {
  const a = useTheme().app;
  const navigate = useNavigate();
  const { userName, logout } = useAuth();
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const close = () => setAnchorEl(null);

  return (
    <>
      <Tooltip title={userName || "Account"}>
        <IconButton onClick={(e) => setAnchorEl(e.currentTarget)} aria-label="Menu utente" sx={{ p: 0.5 }}>
          <Avatar
            sx={{
              width: 32,
              height: 32,
              fontSize: "0.8rem",
              fontWeight: 700,
              bgcolor: a.primarySoft,
              color: a.primary,
              borderRadius: `${Math.max(a.radiusControl, 6)}px`,
            }}
          >
            {initialsOf(userName)}
          </Avatar>
        </IconButton>
      </Tooltip>
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={close}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
        transformOrigin={{ vertical: "top", horizontal: "right" }}
        slotProps={{ paper: { sx: { minWidth: 220 } } }}
      >
        <Box sx={{ px: 2, py: 1 }}>
          <Typography sx={{ fontSize: "0.72rem", color: a.textMuted }}>Connesso come</Typography>
          <Typography sx={{ fontWeight: 600, fontSize: "0.88rem" }} noWrap>
            {userName || "Utente"}
          </Typography>
        </Box>
        <Divider sx={{ my: 0.5 }} />
        <MenuItem
          onClick={() => {
            close();
            navigate(`${ROUTES.settings}?sezione=aspetto`);
          }}
        >
          <ListItemIcon>
            <PaletteOutlined fontSize="small" />
          </ListItemIcon>
          <ListItemText>Aspetto</ListItemText>
        </MenuItem>
        <MenuItem
          onClick={() => {
            close();
            logout();
          }}
          sx={{ color: "error.main" }}
        >
          <ListItemIcon sx={{ color: "inherit" }}>
            <LogoutRounded fontSize="small" />
          </ListItemIcon>
          <ListItemText>Esci</ListItemText>
        </MenuItem>
      </Menu>
    </>
  );
}
