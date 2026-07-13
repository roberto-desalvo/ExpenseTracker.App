import { useEffect, useState } from "react";
import { Box, IconButton, InputAdornment, Stack, TextField, Tooltip } from "@mui/material";
import { Add, Refresh, Search } from "@mui/icons-material";
import { useTheme } from "@mui/material/styles";

interface CategoriesFilterBarProps {
  onSearch: (name?: string) => void;
  onAddClick: () => void;
  onRefresh: () => void;
  isLoading?: boolean;
}

export default function CategoriesFilterBar({
  onSearch,
  onAddClick,
  onRefresh,
  isLoading = false,
}: CategoriesFilterBarProps) {
  const theme = useTheme();
  const c = theme.palette.custom;
  const [searchTerm, setSearchTerm] = useState<string>("");

  const controlBoxSx = {
    height: 40,
    borderRadius: "12px",
    border: `1px solid ${c.filterBorder}`,
    backgroundColor: c.filterBackground,
  };

  const controlLabelColor = c.filterText;

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      const normalized = searchTerm.trim();
      if (normalized.length === 0) {
        onSearch(undefined);
      } else if (normalized.length >= 3) {
        onSearch(normalized);
      }
    }, 350);

    return () => clearTimeout(timeoutId);
  }, [searchTerm]);

  return (
    <Box
      sx={{
        display: "flex",
        flexWrap: "wrap",
        alignItems: "center",
        justifyContent: "flex-start",
        gap: 1.25,
        px: 1.5,
        py: 1.25,
      }}
    >
      <Box sx={{ ...controlBoxSx, minWidth: 280, display: "flex", alignItems: "center", px: 1 }}>
        <TextField
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          placeholder="Filtra per nome"
          size="small"
          disabled={isLoading}
          sx={{
            width: "100%",
            "& .MuiOutlinedInput-notchedOutline": { border: "none" },
            "& .MuiInputBase-input": { color: controlLabelColor, py: 0.75, fontSize: "0.88rem" },
          }}
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <Search fontSize="small" sx={{ color: controlLabelColor }} />
                </InputAdornment>
              ),
            },
          }}
        />
      </Box>

      <Stack
        direction="row"
        spacing={1}
        sx={{ ml: { xs: 0, md: "auto" }, width: { xs: "100%", md: "auto" } }}
      >
        <Tooltip title="Nuova categoria">
          <IconButton
            sx={{ ...controlBoxSx, color: controlLabelColor, px: 1.25 }}
            onClick={onAddClick}
            disabled={isLoading}
          >
            <Add />
          </IconButton>
        </Tooltip>
        <Tooltip title="Aggiorna">
          <IconButton
            sx={{ ...controlBoxSx, color: controlLabelColor, px: 1.25 }}
            onClick={onRefresh}
            disabled={isLoading}
          >
            <Refresh />
          </IconButton>
        </Tooltip>
      </Stack>
    </Box>
  );
}
