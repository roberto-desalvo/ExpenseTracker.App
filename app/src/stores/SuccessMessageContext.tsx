import React, { createContext, useContext, useMemo, useState, ReactNode } from "react";
import { Alert, Snackbar } from "@mui/material";

interface SuccessMessageContextType {
  showSuccess: (message: string) => void;
}

const SuccessMessageContext = createContext<SuccessMessageContextType | undefined>(undefined);

export const SuccessMessageProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [message, setMessage] = useState<string | null>(null);
  const [open, setOpen] = useState<boolean>(false);

  const showSuccess = (successMessage: string) => {
    setMessage(successMessage);
    setOpen(true);
  };

  const contextValue = useMemo(
    () => ({
      showSuccess,
    }),
    []
  );

  return (
    <SuccessMessageContext.Provider value={contextValue}>
      {children}
      <Snackbar
        open={open}
        autoHideDuration={4000}
        onClose={(_event, reason) => {
          if (reason === "clickaway") {
            return;
          }

          setOpen(false);
        }}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      >
        <Alert
          severity="success"
          variant="filled"
          onClose={() => setOpen(false)}
          sx={{ width: "100%", minWidth: 320 }}
        >
          {message ?? "Operazione completata"}
        </Alert>
      </Snackbar>
    </SuccessMessageContext.Provider>
  );
};

export const useSuccessMessage = (): SuccessMessageContextType => {
  const context = useContext(SuccessMessageContext);
  if (!context) {
    throw new Error(
      "useSuccessMessage deve essere utilizzato all'interno di SuccessMessageProvider"
    );
  }
  return context;
};
