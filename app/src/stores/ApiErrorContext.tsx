import React, { createContext, useContext, useMemo, useState, ReactNode, useEffect } from "react";
import { Alert, Snackbar } from "@mui/material";
import { API_ERROR_EVENT } from "../services/ApiClient";

interface ApiErrorContextType {
  showError: (message: string) => void;
}

const ApiErrorContext = createContext<ApiErrorContextType | undefined>(undefined);

export const ApiErrorProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [message, setMessage] = useState<string | null>(null);
  const [open, setOpen] = useState<boolean>(false);

  const showError = (errorMessage: string) => {
    setMessage(errorMessage);
    setOpen(true);
  };

  useEffect(() => {
    const onApiError = (event: Event) => {
      const customEvent = event as CustomEvent<{ message?: string }>;
      const errorMessage = customEvent.detail?.message;

      if (!errorMessage) {
        return;
      }

      showError(errorMessage);
    };

    window.addEventListener(API_ERROR_EVENT, onApiError as EventListener);

    return () => {
      window.removeEventListener(API_ERROR_EVENT, onApiError as EventListener);
    };
  }, []);

  const contextValue = useMemo(
    () => ({
      showError,
    }),
    []
  );

  return (
    <ApiErrorContext.Provider value={contextValue}>
      {children}
      <Snackbar
        open={open}
        autoHideDuration={5000}
        onClose={(_event, reason) => {
          if (reason === "clickaway") {
            return;
          }

          setOpen(false);
        }}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      >
        <Alert
          severity="error"
          variant="filled"
          onClose={() => setOpen(false)}
          sx={{ width: "100%", minWidth: 320 }}
        >
          {message ?? "Si è verificato un errore"}
        </Alert>
      </Snackbar>
    </ApiErrorContext.Provider>
  );
};

export const useApiError = (): ApiErrorContextType => {
  const context = useContext(ApiErrorContext);
  if (!context) {
    throw new Error("useApiError deve essere utilizzato all'interno di ApiErrorProvider");
  }
  return context;
};
