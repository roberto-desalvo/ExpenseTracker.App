import { useEffect, useRef, useState } from "react";
import { IconButton, InputAdornment, TextField } from "@mui/material";
import { CloseRounded, SearchRounded } from "@mui/icons-material";

interface SearchFieldProps {
  placeholder: string;
  /** chiamata con debounce: undefined quando il campo è vuoto, il testo da `minLength` caratteri */
  onSearch: (term?: string) => void;
  minLength?: number;
}

export default function SearchField({ placeholder, onSearch, minLength = 3 }: SearchFieldProps) {
  const [value, setValue] = useState("");
  const isFirstRender = useRef(true);

  useEffect(() => {
    if (isFirstRender.current) {
      isFirstRender.current = false;
      return;
    }

    const timeoutId = setTimeout(() => {
      const normalized = value.trim();
      if (normalized.length === 0) {
        onSearch(undefined);
      } else if (normalized.length >= minLength) {
        onSearch(normalized);
      }
    }, 350);

    return () => clearTimeout(timeoutId);
  }, [value]);

  return (
    <TextField
      size="small"
      value={value}
      onChange={(e) => setValue(e.target.value)}
      placeholder={placeholder}
      sx={{ minWidth: { xs: "100%", sm: 300 } }}
      helperText={value.trim().length > 0 && value.trim().length < minLength ? `Digita almeno ${minLength} caratteri` : undefined}
      slotProps={{
        input: {
          startAdornment: (
            <InputAdornment position="start">
              <SearchRounded fontSize="small" />
            </InputAdornment>
          ),
          endAdornment: value ? (
            <InputAdornment position="end">
              <IconButton size="small" onClick={() => setValue("")} aria-label="Cancella ricerca">
                <CloseRounded fontSize="small" />
              </IconButton>
            </InputAdornment>
          ) : undefined,
        },
      }}
    />
  );
}
