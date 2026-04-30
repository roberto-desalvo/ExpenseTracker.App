import { useState } from "react";
import { Stack, TableCell, TableRow, TextField, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { EditRounded } from "@mui/icons-material";
import Account from "../models/Account";
import DataTableBase, { DataTableColumn } from "../components/DataTableBase";
import RowActionsMenu from "../components/RowActionsMenu";
import AppModal from "../components/AppModal";
import AccountsFilterBar from "../components/AccountsFilterBar";
import { useAccounts } from "../stores/AccountContext";

type AccountFormState = {
  name: string;
};

const createInitialForm = (): AccountFormState => ({
  name: "",
});

export default function AccountsPage() {
  const theme = useTheme();
  const c = theme.palette.custom;
  const {
    pagedAccounts,
    isLoading,
    page,
    pageSize,
    totalCount,
    modifyPage,
    modifyPageSize,
    addAccount,
    updateAccount,
    refreshAccounts,
  } = useAccounts();

  const [form, setForm] = useState<AccountFormState>(createInitialForm());
  const [modalOpen, setModalOpen] = useState<boolean>(false);
  const [editingAccount, setEditingAccount] = useState<Account | null>(null);
  const [operationInProgress, setOperationInProgress] = useState<boolean>(false);

  const columns: DataTableColumn[] = [
    { id: "name", label: "Nome", minWidth: 200 },
    { id: "actions", label: "Azioni", minWidth: 80, align: "center" },
  ];

  const handleSearch = (name?: string) => {
    modifyPage(0);
    void refreshAccounts(name);
  };

  const openCreateModal = () => {
    setEditingAccount(null);
    setForm(createInitialForm());
    setModalOpen(true);
  };

  const openEditModal = (account: Account) => {
    setEditingAccount(account);
    setForm({
      name: account.name,
    });
    setModalOpen(true);
  };

  const closeModal = () => {
    setModalOpen(false);
  };

  const handleSubmit = async (event: React.FormEvent): Promise<string | void> => {
    event.preventDefault();

    const payload: Account = {
      id: editingAccount?.id ?? 0,
      name: form.name.trim(),
    };

    if (payload.name.length === 0) {
      return;
    }

    setOperationInProgress(true);
    try {
      if (payload.id > 0) {
        await updateAccount(payload);
        return `Account "${payload.name}" modificato`;
      }

      await addAccount(payload);
      return `Account "${payload.name}" creato`;
    } finally {
      setOperationInProgress(false);
    }
  };

  const renderAccountRow = (account: Account) => (
    <TableRow
      hover
      key={account.id}
      sx={{
        "& td:first-of-type": {
          borderTopLeftRadius: 8,
          borderBottomLeftRadius: 8,
        },
        "& td:last-of-type": {
          borderTopRightRadius: 8,
          borderBottomRightRadius: 8,
        },
        "&:hover td": {
          backgroundColor: c.rowHover,
        },
      }}
    >
      <TableCell
        sx={{
          borderBottom: "none",
          backgroundColor: c.rowBackground,
          py: 1.5,
        }}
      >
        <Stack direction="row" spacing={1} alignItems="center">
          <Typography sx={{ color: "text.primary", fontWeight: 500 }}>
            {account.name}
          </Typography>
        </Stack>
      </TableCell>
      <TableCell
        align="center"
        sx={{
          borderBottom: "none",
          backgroundColor: c.rowBackground,
          py: 1.5,
        }}
      >
        <RowActionsMenu
          rowId={account.id}
          ariaLabel="Azioni account"
          actions={[
            {
              label: "Modifica",
              icon: <EditRounded fontSize="small" />,
              onClick: () => openEditModal(account),
            },
          ]}
        />
      </TableCell>
    </TableRow>
  );

  return (
    <>
      <AccountsFilterBar
        onSearch={handleSearch}
        onAddClick={openCreateModal}
        onRefresh={() => void refreshAccounts()}
        isLoading={isLoading}
      />
      <main className="flex-1 min-h-0 px-2 pb-2">
        <DataTableBase
          title="Account"
          columns={columns}
          rows={pagedAccounts}
          isLoading={isLoading}
          isEmpty={!isLoading && pagedAccounts.length === 0}
          emptyMessage="Nessun account trovato"
          emptySubtext="Crea il tuo primo account per iniziare"
          page={page}
          pageSize={pageSize}
          totalCount={totalCount}
          onPageChange={(_event, newPage) => modifyPage(newPage)}
          onPageSizeChange={(event) =>
            modifyPageSize(parseInt(event.target.value, 10))
          }
          renderRow={(account) => renderAccountRow(account)}
        />

        <AppModal
          open={modalOpen}
          onClose={closeModal}
          title={editingAccount ? "Modifica account" : "Nuovo account"}
          onSubmit={handleSubmit}
          isBusy={operationInProgress}
          submitLabel={editingAccount ? "Salva modifiche" : "Crea account"}
        >
          <TextField
            label="Nome"
            value={form.name}
            onChange={(event) =>
              setForm((prev) => ({ ...prev, name: event.target.value }))
            }
            required
            fullWidth
            disabled={operationInProgress}
          />
        </AppModal>
      </main>
    </>
  );
}
