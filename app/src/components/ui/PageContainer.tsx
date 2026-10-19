import { ReactNode } from "react";
import { Stack } from "@mui/material";

/** Larghezza massima e spaziature comuni a tutte le pagine. */
export default function PageContainer({ children }: { children: ReactNode }) {
  return (
    <Stack
      spacing={{ xs: 2, md: 2.5 }}
      sx={{
        flex: 1,
        width: "100%",
        maxWidth: 1440,
        mx: "auto",
        px: { xs: 2, md: 3.5 },
        pt: { xs: 2.5, md: 3.5 },
        pb: 6,
        minWidth: 0,
      }}
    >
      {children}
    </Stack>
  );
}
