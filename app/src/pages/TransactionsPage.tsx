import { useState } from "react";
import { Alert, Box, Stack, Tab, Tabs, Typography } from "@mui/material";
import AccountsBar from "../components/AccountsBar";
import ExpenseTable from "../components/ExpenseTable";
import TransactionModal from "../components/TransactionModal";
import TransactionsSummaryBar from "../components/TransactionsSummaryBar";

export default function TransactionsPage() {
  const [activeTab, setActiveTab] = useState<number>(0);

  return (
    <>
      <Stack spacing={2.5} sx={{ flex: 1, px: { xs: 2.5, md: 4 }, py: { xs: 2.5, md: 3 } }}>
        <Typography
          variant="h5"
          sx={{
            color: "text.primary",
            fontWeight: 700,
            letterSpacing: "-0.02em",
            px: 1,
          }}
        >
          Transazioni
        </Typography>
        <Box sx={{ px: { xs: 0.5, md: 1 } }}>
          <Tabs
            value={activeTab}
            onChange={(_event, value: number) => setActiveTab(value)}
            textColor="inherit"
            indicatorColor="primary"
          >
            <Tab label="Gestione" />
            <Tab label="Analisi" />
          </Tabs>
        </Box>
        {activeTab === 0 ? (
          <>
            <TransactionsSummaryBar />
            <AccountsBar />
            <main className="px-2 pb-2">
              <ExpenseTable />
            </main>
          </>
        ) : (
          <Box sx={{ px: { xs: 0.5, md: 1 } }}>
            <Alert severity="info">
              La sezione analisi delle transazioni sarà disponibile a breve.
            </Alert>
          </Box>
        )}
      </Stack>
      <TransactionModal />
    </>
  );
}
