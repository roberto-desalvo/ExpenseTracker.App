import { useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { useTheme } from "@mui/material/styles";
import {
  Box,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemText,
  Divider,
  Typography,
} from "@mui/material";
import { Menu as MenuIcon } from "@mui/icons-material";
import LogoutIcon from "@mui/icons-material/Logout";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";

const navItems = [
  { label: "Home", path: "/" },
  { label: "Transazioni", path: "/transazioni" },
  { label: "Categorie", path: "/categorie" },
  { label: "Account", path: "/account" },
];

export default function HomeHeader() {
  const [open, setOpen] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const theme = useTheme();
  const c = theme.palette.custom;
  const { instance, accounts } = useMsal();
  const isAuthenticated = useIsAuthenticated();

  const userName = accounts[0]?.name ?? accounts[0]?.username ?? "";

  const handleNavigate = (path: string) => {
    navigate(path);
    setOpen(false);
  };

  const handleLogout = () => {
    setOpen(false);
    instance.logoutRedirect();
  };

  return (
    <>
      <Box
        component="header"
        sx={{
          display: "grid",
          gridTemplateColumns: "44px 1fr 44px",
          alignItems: "center",
          py: 1.5,
          px: 2,
          backgroundColor:
            theme.palette.mode === "light"
              ? "rgba(241, 245, 249, 0.92)"
              : "rgba(15, 23, 42, 0.9)",
          borderBottom: `1px solid ${theme.palette.divider}`,
          backdropFilter: "blur(12px)",
          boxShadow:
            theme.palette.mode === "light"
              ? "0 1px 0 rgba(255,255,255,0.65) inset"
              : "0 1px 0 rgba(255,255,255,0.03) inset",
        }}
      >
        <IconButton
          onClick={() => setOpen(true)}
          sx={{
            justifySelf: "start",
            color: "text.secondary",
            borderRadius: "10px",
            border: `1px solid ${c.filterBorder}`,
            backgroundColor: c.filterBackground,
            p: 1,
            "&:hover": { backgroundColor: c.rowHover },
          }}
        >
          <MenuIcon fontSize="small" />
        </IconButton>
        <Typography
          onClick={() => handleNavigate("/")}
          sx={{
            color: "text.primary",
            fontWeight: 700,
            fontSize: "1.02rem",
            letterSpacing: "0.02em",
            textAlign: "center",
            cursor: "pointer",
          }}
        >
          Expense Tracker
        </Typography>
        <Box sx={{ width: 44, height: 44 }} />
      </Box>

      <Drawer
        anchor="left"
        open={open}
        onClose={() => setOpen(false)}
        PaperProps={{
          sx: {
            backgroundColor: c.drawerBackground,
            color: theme.palette.text.primary,
            borderRight: `1px solid ${c.drawerBorder}`,
            width: 240,
          },
        }}
      >
        <List sx={{ pt: 2 }}>
          <ListItemText
            primary="Menu"
            sx={{
              px: 3,
              pb: 1,
              "& .MuiListItemText-primary": {
                fontWeight: 700,
                color: c.accentColor,
                fontSize: "0.88rem",
                textTransform: "uppercase",
                letterSpacing: "0.08em",
              },
            }}
          />
          <Divider sx={{ borderColor: c.drawerBorder, mb: 1 }} />
          {isAuthenticated &&
            navItems.map((item) => (
              <ListItemButton
                key={item.path}
                selected={location.pathname === item.path}
                onClick={() => handleNavigate(item.path)}
                sx={{
                  mx: 1,
                  borderRadius: "8px",
                  color: theme.palette.text.secondary,
                  "&.Mui-selected": {
                    backgroundColor: c.accentHover,
                    color: c.accentColor,
                    "& .MuiListItemText-primary": { fontWeight: 600 },
                  },
                  "&:hover": { backgroundColor: c.rowHover },
                }}
              >
                <ListItemText primary={item.label} />
              </ListItemButton>
            ))}
          {isAuthenticated && (
            <>
              <Divider sx={{ borderColor: c.drawerBorder, mt: 1 }} />
              {userName && (
                <Typography
                  sx={{
                    px: 3,
                    pt: 1.5,
                    pb: 0.5,
                    fontSize: "0.78rem",
                    color: "text.disabled",
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                    whiteSpace: "nowrap",
                  }}
                >
                  {userName}
                </Typography>
              )}
              <ListItemButton
                onClick={handleLogout}
                sx={{
                  mx: 1,
                  borderRadius: "8px",
                  color: theme.palette.error.main,
                  "&:hover": { backgroundColor: c.rowHover },
                }}
              >
                <LogoutIcon fontSize="small" sx={{ mr: 1.5 }} />
                <ListItemText primary="Esci" />
              </ListItemButton>
            </>
          )}
        </List>
      </Drawer>
    </>
  );
}
