import { useState } from "react";
import {
  IconButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
} from "@mui/material";
import { MoreVertRounded } from "@mui/icons-material";
import { useTheme } from "@mui/material/styles";

export interface RowAction {
  label: string;
  icon: React.ReactNode;
  onClick: () => void;
  disabled?: boolean;
}

interface RowActionsMenuProps {
  actions: RowAction[];
  ariaLabel?: string;
  rowId?: number | string;
}

export default function RowActionsMenu({
  actions,
  ariaLabel = "Azioni riga",
  rowId,
}: RowActionsMenuProps) {
  const theme = useTheme();
  const c = theme.palette.custom;
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const isOpen = Boolean(anchorEl);
  const menuId = rowId !== undefined ? `row-actions-menu-${rowId}` : "row-actions-menu";

  const handleOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  return (
    <>
      <IconButton
        aria-label={ariaLabel}
        aria-controls={isOpen ? menuId : undefined}
        aria-expanded={isOpen ? "true" : undefined}
        aria-haspopup="true"
        onClick={handleOpen}
        sx={{
          color: c.actionButtonColor,
          border: `1px solid ${c.actionButtonBorder}`,
          backgroundColor: c.actionButtonBackground,
          "&:hover": {
            color: theme.palette.text.secondary,
            backgroundColor: c.actionButtonHover,
          },
        }}
      >
        <MoreVertRounded />
      </IconButton>
      <Menu
        id={menuId}
        anchorEl={anchorEl}
        open={isOpen}
        onClose={handleClose}
      >
        {actions.map((action) => (
          <MenuItem
            key={action.label}
            onClick={() => {
              action.onClick();
              handleClose();
            }}
            disabled={action.disabled}
          >
            <ListItemIcon>{action.icon}</ListItemIcon>
            <ListItemText>{action.label}</ListItemText>
          </MenuItem>
        ))}
      </Menu>
    </>
  );
}
