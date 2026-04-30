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

const navItems = [
  { label: "Transazioni", path: "/" },
  { label: "Categorie", path: "/categorie" },
  { label: "Account", path: "/account" },
];

export default function HomeHeader() {
  const [open, setOpen] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const theme = useTheme();
  const c = theme.palette.custom;

  const handleNavigate = (path: string) => {
    navigate(path);
    setOpen(false);
  };

  return (
    <>
      <Box
        component="header"
        sx={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          py: 1.5,
          px: 2,
          bgcolor: "background.default",
          borderBottom: `1px solid ${theme.palette.divider}`,
        }}
      >
        <Typography
          sx={{
            color: "text.primary",
            fontWeight: 600,
            fontSize: "1rem",
            letterSpacing: "0.01em",
          }}
        >
          Expense Tracker
        </Typography>
        <IconButton
          onClick={() => setOpen(true)}
          sx={{
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
      </Box>

      <Drawer
        anchor="right"
        open={open}
        onClose={() => setOpen(false)}
        PaperProps={{
          sx: {
            backgroundColor: c.drawerBackground,
            color: theme.palette.text.primary,
            borderLeft: `1px solid ${c.drawerBorder}`,
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
          {navItems.map((item) => (
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
        </List>
      </Drawer>
    </>
  );
}
