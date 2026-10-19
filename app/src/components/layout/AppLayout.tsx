import { ReactNode, useEffect, useState } from "react";
import {
  Box,
  ButtonBase,
  Drawer,
  IconButton,
  Stack,
  Tooltip,
  Typography,
  useMediaQuery,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import {
  KeyboardDoubleArrowLeftRounded,
  KeyboardDoubleArrowRightRounded,
  LogoutRounded,
  MenuRounded,
} from "@mui/icons-material";
import { useLocation, useNavigate } from "react-router-dom";
import { useAppTheme } from "../../stores/ThemeContext";
import { useAuth } from "../../auth/AuthContext";
import BrandMark from "./BrandMark";
import { NAV_ITEMS, isNavItemActive } from "./navigation";
import { NewEntryButton, ThemeModeButton, UserMenuButton } from "./HeaderActions";
import { initialsOf } from "../../utilities/format.utilities";

const SIDEBAR_WIDTH = 256;
const SIDEBAR_COLLAPSED_WIDTH = 76;
const COLLAPSED_STORAGE_KEY = "expense-tracker:sidebar-collapsed";

const readCollapsed = () => {
  try {
    return window.localStorage.getItem(COLLAPSED_STORAGE_KEY) === "true";
  } catch {
    return false;
  }
};

function SidebarNav({ collapsed, onNavigate }: { collapsed: boolean; onNavigate: (path: string) => void }) {
  const a = useTheme().app;
  const location = useLocation();

  return (
    <Stack spacing={0.5} component="nav" aria-label="Navigazione principale">
      {NAV_ITEMS.map((item) => {
        const active = isNavItemActive(item, location.pathname);
        const button = (
          <ButtonBase
            key={item.path}
            onClick={() => onNavigate(item.path)}
            aria-current={active ? "page" : undefined}
            sx={{
              justifyContent: collapsed ? "center" : "flex-start",
              gap: 1.5,
              px: collapsed ? 0 : 1.5,
              height: 42,
              width: "100%",
              borderRadius: `${a.radiusControl}px`,
              color: active ? a.text : a.textSecondary,
              backgroundColor: active ? a.primarySoft : "transparent",
              fontWeight: active ? 650 : 500,
              fontSize: "0.9rem",
              position: "relative",
              transition: "background-color .15s ease, color .15s ease",
              "&:hover": { backgroundColor: active ? a.primarySoft : a.surfaceAlt, color: a.text },
              "& .nav-icon": { color: active ? a.primary : "inherit", display: "flex" },
              "&::before": active
                ? {
                    content: '""',
                    position: "absolute",
                    left: collapsed ? 6 : -12,
                    top: 10,
                    bottom: 10,
                    width: 3,
                    borderRadius: 3,
                    backgroundColor: a.primary,
                  }
                : undefined,
            }}
          >
            <Box className="nav-icon">{item.icon}</Box>
            {!collapsed && <span>{item.label}</span>}
          </ButtonBase>
        );

        return collapsed ? (
          <Tooltip key={item.path} title={item.label} placement="right">
            {button}
          </Tooltip>
        ) : (
          button
        );
      })}
    </Stack>
  );
}

function SidebarContent({
  collapsed,
  onToggleCollapsed,
  onNavigate,
}: {
  collapsed: boolean;
  onToggleCollapsed?: () => void;
  onNavigate: (path: string) => void;
}) {
  const a = useTheme().app;
  const { userName, logout } = useAuth();

  return (
    <Stack sx={{ height: "100%", px: collapsed ? 1.5 : 2, py: 2.5 }} spacing={3}>
      <Box sx={{ px: collapsed ? 0.5 : 0.5, display: "flex", justifyContent: collapsed ? "center" : "flex-start" }}>
        <BrandMark compact={collapsed} onClick={() => onNavigate("/")} />
      </Box>

      <Box sx={{ display: "flex", justifyContent: "center" }}>
        <NewEntryButton iconOnly={collapsed} fullWidth />
      </Box>

      <Box>
        {!collapsed && (
          <Typography variant="overline" sx={{ color: a.textMuted, px: 1.5, fontSize: "0.66rem" }}>
            Menu
          </Typography>
        )}
        <SidebarNav collapsed={collapsed} onNavigate={onNavigate} />
      </Box>

      <Box sx={{ flex: 1 }} />

      <Stack spacing={1}>
        <Stack
          direction={collapsed ? "column" : "row"}
          alignItems="center"
          justifyContent="space-between"
          spacing={0.5}
        >
          <ThemeModeButton />
          {onToggleCollapsed && (
            <Tooltip title={collapsed ? "Espandi menu" : "Comprimi menu"} placement="right">
              <IconButton onClick={onToggleCollapsed} sx={{ color: "text.secondary" }} aria-label="Comprimi menu">
                {collapsed ? (
                  <KeyboardDoubleArrowRightRounded fontSize="small" />
                ) : (
                  <KeyboardDoubleArrowLeftRounded fontSize="small" />
                )}
              </IconButton>
            </Tooltip>
          )}
        </Stack>

        <Stack
          direction="row"
          alignItems="center"
          spacing={1.25}
          sx={{
            p: collapsed ? 0 : 1,
            borderRadius: `${a.radiusControl + 2}px`,
            border: collapsed ? "none" : `1px solid ${a.border}`,
            backgroundColor: collapsed ? "transparent" : a.surfaceAlt,
            justifyContent: "center",
          }}
        >
          <Box
            sx={{
              width: 32,
              height: 32,
              flexShrink: 0,
              borderRadius: `${Math.max(a.radiusControl, 6)}px`,
              display: "grid",
              placeItems: "center",
              bgcolor: a.primarySoft,
              color: a.primary,
              fontWeight: 700,
              fontSize: "0.78rem",
            }}
          >
            {initialsOf(userName)}
          </Box>
          {!collapsed && (
            <>
              <Typography sx={{ flex: 1, minWidth: 0, fontSize: "0.82rem", fontWeight: 600 }} noWrap>
                {userName || "Utente"}
              </Typography>
              <Tooltip title="Esci">
                <IconButton size="small" onClick={logout} aria-label="Esci" sx={{ color: "text.secondary" }}>
                  <LogoutRounded fontSize="small" />
                </IconButton>
              </Tooltip>
            </>
          )}
        </Stack>
      </Stack>
    </Stack>
  );
}

function TopNav({ onNavigate }: { onNavigate: (path: string) => void }) {
  const a = useTheme().app;
  const location = useLocation();

  return (
    <Stack direction="row" spacing={0.5} component="nav" aria-label="Navigazione principale">
      {NAV_ITEMS.map((item) => {
        const active = isNavItemActive(item, location.pathname);
        return (
          <ButtonBase
            key={item.path}
            onClick={() => onNavigate(item.path)}
            aria-current={active ? "page" : undefined}
            sx={{
              gap: 1,
              px: 1.5,
              height: 38,
              borderRadius: `${a.radiusControl}px`,
              fontSize: "0.88rem",
              fontWeight: active ? 650 : 500,
              color: active ? a.text : a.textSecondary,
              backgroundColor: active ? a.surfaceAlt : "transparent",
              "&:hover": { color: a.text, backgroundColor: a.surfaceAlt },
              "& svg": { color: active ? a.primary : "inherit" },
            }}
          >
            {item.icon}
            {item.label}
          </ButtonBase>
        );
      })}
    </Stack>
  );
}

export default function AppLayout({ children }: { children: ReactNode }) {
  const theme = useTheme();
  const a = theme.app;
  const navigate = useNavigate();
  const location = useLocation();
  const { preferences } = useAppTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up("md"));
  const [mobileOpen, setMobileOpen] = useState(false);
  const [collapsed, setCollapsed] = useState<boolean>(readCollapsed);

  const useSidebar = preferences.nav === "sidebar" && isDesktop;
  const useTopbar = preferences.nav === "topbar" && isDesktop;
  const sidebarWidth = collapsed ? SIDEBAR_COLLAPSED_WIDTH : SIDEBAR_WIDTH;

  useEffect(() => {
    setMobileOpen(false);
    window.scrollTo({ top: 0 });
  }, [location.pathname]);

  const toggleCollapsed = () => {
    setCollapsed((prev) => {
      try {
        window.localStorage.setItem(COLLAPSED_STORAGE_KEY, String(!prev));
      } catch {
        // ignora
      }
      return !prev;
    });
  };

  const headerSx = {
    position: "sticky" as const,
    top: 0,
    zIndex: theme.zIndex.appBar,
    backdropFilter: "saturate(180%) blur(14px)",
    backgroundColor: theme.palette.mode === "dark" ? "rgba(10,10,14,0.72)" : "rgba(255,255,255,0.78)",
    borderBottom: `1px solid ${a.border}`,
  };

  return (
    <Box sx={{ display: "flex", minHeight: "100vh", width: "100%", bgcolor: a.background }}>
      {useSidebar && (
        <Box
          component="aside"
          sx={{
            width: sidebarWidth,
            flexShrink: 0,
            transition: "width .2s ease",
            position: "sticky",
            top: 0,
            height: "100vh",
            borderRight: `1px solid ${a.border}`,
            backgroundColor: a.sidebarBackground,
            overflowY: "auto",
            overflowX: "hidden",
          }}
        >
          <SidebarContent collapsed={collapsed} onToggleCollapsed={toggleCollapsed} onNavigate={navigate} />
        </Box>
      )}

      <Box sx={{ flex: 1, minWidth: 0, display: "flex", flexDirection: "column" }}>
        {useTopbar && (
          <Box component="header" sx={headerSx}>
            <Stack
              direction="row"
              alignItems="center"
              spacing={3}
              sx={{ maxWidth: 1440, mx: "auto", px: 3.5, height: 64 }}
            >
              <BrandMark onClick={() => navigate("/")} />
              <TopNav onNavigate={navigate} />
              <Box sx={{ flex: 1 }} />
              <Stack direction="row" alignItems="center" spacing={1}>
                <ThemeModeButton />
                <NewEntryButton />
                <UserMenuButton />
              </Stack>
            </Stack>
          </Box>
        )}

        {!isDesktop && (
          <Box component="header" sx={headerSx}>
            <Stack direction="row" alignItems="center" spacing={1} sx={{ px: 1.5, height: 58 }}>
              <IconButton onClick={() => setMobileOpen(true)} aria-label="Apri menu" sx={{ color: "text.secondary" }}>
                <MenuRounded />
              </IconButton>
              <BrandMark onClick={() => navigate("/")} />
              <Box sx={{ flex: 1 }} />
              <ThemeModeButton />
              <NewEntryButton iconOnly />
            </Stack>
          </Box>
        )}

        <Box component="main" sx={{ flex: 1, display: "flex", flexDirection: "column", minWidth: 0 }}>
          {children}
        </Box>
      </Box>

      {!isDesktop && (
        <Drawer
          anchor="left"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          PaperProps={{
            sx: { width: 280, backgroundColor: a.sidebarBackground, borderRight: `1px solid ${a.border}` },
          }}
        >
          <SidebarContent collapsed={false} onNavigate={navigate} />
        </Drawer>
      )}
    </Box>
  );
}
