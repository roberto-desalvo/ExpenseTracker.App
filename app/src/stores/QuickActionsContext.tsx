import React, { createContext, ReactNode, useContext, useMemo, useState } from "react";
import TransactionModal from "../components/TransactionModal";
import TransferModal from "../components/TransferModal";
import { useTransactionModal } from "./TransactionModalContext";

interface QuickActionsContextType {
  openNewTransaction: () => void;
  openNewTransfer: () => void;
}

const QuickActionsContext = createContext<QuickActionsContextType | undefined>(undefined);

/**
 * Monta una sola volta le modali di inserimento (transazione e trasferimento)
 * e le rende apribili da qualunque pagina (header, dashboard, movimenti).
 */
export const QuickActionsProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const { openTransactionModal } = useTransactionModal();
  const [transferOpen, setTransferOpen] = useState(false);

  const value = useMemo<QuickActionsContextType>(
    () => ({
      openNewTransaction: () => openTransactionModal(),
      openNewTransfer: () => setTransferOpen(true),
    }),
    [openTransactionModal],
  );

  return (
    <QuickActionsContext.Provider value={value}>
      {children}
      <TransactionModal />
      <TransferModal open={transferOpen} onClose={() => setTransferOpen(false)} />
    </QuickActionsContext.Provider>
  );
};

export const useQuickActions = (): QuickActionsContextType => {
  const context = useContext(QuickActionsContext);
  if (!context) {
    throw new Error("useQuickActions deve essere utilizzato all'interno di QuickActionsProvider");
  }
  return context;
};
