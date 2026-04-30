import {
  Box,
  Checkbox,
  FormControl,
  IconButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Select,
  SelectChangeEvent,
  Stack,
  Tooltip,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import {
  Add,
  CompareArrows,
  Refresh,
  ReceiptLong,
} from "@mui/icons-material";
import { MouseEvent, useState } from "react";
import { useAccounts } from "../stores/AccountContext";
import { useCategories } from "../stores/CategoryContext";
import { useTableContext } from "../stores/TableContext";
import { useTransactions } from "../stores/TransactionContext";
import { useTransactionModal } from "../stores/TransactionModalContext";
import TransferModal from "./TransferModal";

export default function AccountsBar() {
  const theme = useTheme();
  const c = theme.palette.custom;
  const accountsContext = useAccounts();
  const categoriesContext = useCategories();
  const tableContext = useTableContext();
  const transactionsContext = useTransactions();
  const transactionModalContext = useTransactionModal();
  const [addMenuAnchorEl, setAddMenuAnchorEl] = useState<null | HTMLElement>(null);
  const [transferModalOpen, setTransferModalOpen] = useState<boolean>(false);
  const allAccountsOptionValue = "__all_accounts__";
  const allCategoriesOptionValue = "__all_categories__";
  const addMenuOpen = Boolean(addMenuAnchorEl);

  const controlBoxSx = {
    height: 44,
    borderRadius: "10px",
    border: `1px solid ${c.filterBorder}`,
    backgroundColor: c.filterBackground,
    display: "flex",
    alignItems: "center",
    px: 1.5,
  };

  const controlLabelColor = c.filterText;

  const menuProps = {
    anchorOrigin: { vertical: "bottom" as const, horizontal: "left" as const },
    transformOrigin: { vertical: "top" as const, horizontal: "left" as const },
    disableScrollLock: true,
    PaperProps: {
      sx: {
        mt: 0.5,
        maxHeight: 300,
        overflowY: "auto",
        backgroundColor: c.drawerBackground,
        border: `1px solid ${c.drawerBorder}`,
        borderRadius: "10px",
        boxShadow: theme.palette.mode === "dark" ? "0 8px 32px rgba(0,0,0,0.4)" : "0 8px 24px rgba(0,0,0,0.1)",
        color: c.filterText,
        "& .MuiMenuItem-root": {
          fontSize: "0.88rem",
          "&:hover": {
            backgroundColor: theme.palette.mode === "dark" ? "rgba(255,255,255,0.06)" : "rgba(30,41,59,0.05)",
          },
          "&.Mui-selected": {
            backgroundColor: theme.palette.mode === "dark" ? "rgba(255,255,255,0.08)" : "rgba(30,41,59,0.08)",
            "&:hover": {
              backgroundColor: theme.palette.mode === "dark" ? "rgba(255,255,255,0.10)" : "rgba(30,41,59,0.10)",
            },
          },
        },
        "& .MuiCheckbox-root": {
          color: theme.palette.text.secondary,
          "&.Mui-checked, &.MuiCheckbox-indeterminate": {
            color: c.accentColor,
          },
        },
        "& .MuiListItemText-primary": {
          color: theme.palette.text.primary,
          fontSize: "0.88rem",
        },
      },
    },
  };

  const isAllSelected =
    accountsContext.accounts.length > 0 &&
    tableContext.selectedAccountIds.length === accountsContext.accounts.length;

  const isAllCategoriesSelected =
    categoriesContext.categories.length > 0 &&
    tableContext.selectedCategoryIds.length === categoriesContext.categories.length;

  const handleAccountChange = (event: SelectChangeEvent<string[]>) => {
    const value = event.target.value;
    const selectedValues =
      typeof value === "string"
        ? value.split(",").filter(Boolean)
        : value;

    if (selectedValues.includes(allAccountsOptionValue)) {
      tableContext.modifySelectedAccountIds(
        isAllSelected
          ? []
          : accountsContext.accounts.map((account) => account.id)
      );
      return;
    }

    const selectedIds = selectedValues
      .map((selected) => Number(selected))
      .filter((id) => Number.isInteger(id));

    tableContext.modifySelectedAccountIds(selectedIds);
  };

  const handleCategoryChange = (event: SelectChangeEvent<string[]>) => {
    const value = event.target.value;
    const selectedValues =
      typeof value === "string"
        ? value.split(",").filter(Boolean)
        : value;

    if (selectedValues.includes(allCategoriesOptionValue)) {
      tableContext.modifySelectedCategoryIds(
        isAllCategoriesSelected
          ? []
          : categoriesContext.categories.map((category) => category.id)
      );
      return;
    }

    const selectedIds = selectedValues
      .map((selected) => Number(selected))
      .filter((id) => Number.isInteger(id));

    tableContext.modifySelectedCategoryIds(selectedIds);
  };

  const handleMonthChange = (event: SelectChangeEvent<string>) => {
    const selectedMonth = tableContext.availableMonths.find(
      (month) => month.startDate === event.target.value,
    );

    if (!selectedMonth) {
      return;
    }

    tableContext.modifySelectedMonth(selectedMonth);
  };

  const handleOpenAddMenu = (event: MouseEvent<HTMLElement>) => {
    setAddMenuAnchorEl(event.currentTarget);
  };

  const handleCloseAddMenu = () => {
    setAddMenuAnchorEl(null);
  };

  const handleAddTransaction = () => {
    handleCloseAddMenu();
    transactionModalContext.openTransactionModal();
  };

  const handleAddTransfer = () => {
    handleCloseAddMenu();
    setTransferModalOpen(true);
  };

  return (
    <>
      <div className="text-white flex flex-wrap items-center justify-start gap-3 px-3 py-3">
        <Box sx={{ ...controlBoxSx, minWidth: 300 }}>
          <FormControl size="small" sx={{ minWidth: 260, width: "100%" }}>
            <Select
              multiple
              value={tableContext.selectedAccountIds.map(String)}
              displayEmpty
              onChange={handleAccountChange}
              MenuProps={menuProps}
              sx={{
                color: controlLabelColor,
                ".MuiOutlinedInput-notchedOutline": {
                  border: "none",
                },
                ".MuiSelect-select": {
                  display: "flex",
                  alignItems: "center",
                  py: 1,
                },
                ".MuiSelect-icon": { color: controlLabelColor },
              }}
              renderValue={(selected) => {
                if (
                  selected.length === 0 ||
                  selected.length === accountsContext.accounts.length
                ) {
                  return "Tutti gli account";
                }

                const names = accountsContext.accounts
                  .filter((account) => selected.includes(String(account.id)))
                  .map((account) => account.name);

                return names.join(", ");
              }}
            >
              <MenuItem value={allAccountsOptionValue}>
                <Checkbox
                  checked={isAllSelected}
                  indeterminate={
                    tableContext.selectedAccountIds.length > 0 && !isAllSelected
                  }
                />
                <ListItemText primary="Tutti gli account" />
              </MenuItem>
              {accountsContext.accounts.map((account) => (
                <MenuItem key={account.id} value={String(account.id)}>
                  <Checkbox
                    checked={tableContext.selectedAccountIds.includes(account.id)}
                  />
                  <ListItemText primary={account.name} />
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Box>

        <Box sx={{ ...controlBoxSx, minWidth: 300 }}>
          <FormControl size="small" sx={{ minWidth: 260, width: "100%" }}>
            <Select
              multiple
              value={tableContext.selectedCategoryIds.map(String)}
              displayEmpty
              onChange={handleCategoryChange}
              MenuProps={menuProps}
              sx={{
                color: controlLabelColor,
                ".MuiOutlinedInput-notchedOutline": {
                  border: "none",
                },
                ".MuiSelect-select": {
                  display: "flex",
                  alignItems: "center",
                  py: 1,
                },
                ".MuiSelect-icon": { color: controlLabelColor },
              }}
              renderValue={(selected) => {
                if (
                  selected.length === 0 ||
                  selected.length === categoriesContext.categories.length
                ) {
                  return "Tutte le categorie";
                }

                const names = categoriesContext.categories
                  .filter((category) => selected.includes(String(category.id)))
                  .map((category) => category.name);

                return names.join(", ");
              }}
            >
              <MenuItem value={allCategoriesOptionValue}>
                <Checkbox
                  checked={isAllCategoriesSelected}
                  indeterminate={
                    tableContext.selectedCategoryIds.length > 0 &&
                    !isAllCategoriesSelected
                  }
                />
                <ListItemText primary="Tutte le categorie" />
              </MenuItem>
              {categoriesContext.categories.map((category) => (
                <MenuItem key={category.id} value={String(category.id)}>
                  <Checkbox
                    checked={tableContext.selectedCategoryIds.includes(category.id)}
                  />
                  <ListItemText primary={category.name} />
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Box>

        <Box sx={{ ...controlBoxSx, minWidth: 240 }}>
          <FormControl size="small" sx={{ minWidth: 200, width: "100%" }}>
            <Select
              displayEmpty
              value={tableContext.selectedMonth?.startDate ?? ""}
              onChange={handleMonthChange}
              MenuProps={menuProps}
              disabled={
                tableContext.availableMonthsLoading ||
                tableContext.availableMonths.length === 0
              }
              sx={{
                color: controlLabelColor,
                ".MuiOutlinedInput-notchedOutline": {
                  border: "none",
                },
                ".MuiSelect-select": {
                  display: "flex",
                  alignItems: "center",
                  py: 1,
                },
                ".MuiSelect-icon": { color: controlLabelColor },
              }}
              renderValue={(selected) => {
                const selectedMonth = tableContext.availableMonths.find(
                  (month) => month.startDate === selected,
                );

                return selectedMonth?.description ?? "Seleziona mese";
              }}
            >
              {tableContext.availableMonths.map((month) => (
                <MenuItem key={month.startDate} value={month.startDate}>
                  {month.description}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Box>

        <Stack
          direction="row"
          spacing={1}
          sx={{ ml: { xs: 0, md: "auto" }, width: { xs: "100%", md: "auto" } }}
        >
          <Tooltip title="Aggiungi">
            <IconButton
              sx={{ ...controlBoxSx, color: controlLabelColor, px: 1.5 }}
              onClick={handleOpenAddMenu}
            >
              <Add />
            </IconButton>
          </Tooltip>
          <Menu
            anchorEl={addMenuAnchorEl}
            open={addMenuOpen}
            onClose={handleCloseAddMenu}
            anchorOrigin={{ vertical: "bottom", horizontal: "left" }}
            transformOrigin={{ vertical: "top", horizontal: "left" }}
            PaperProps={{
              sx: {
                mt: 0.5,
                borderRadius: "10px",
                border: `1px solid ${c.drawerBorder}`,
                backgroundColor: c.drawerBackground,
                color: theme.palette.text.primary,
              },
            }}
          >
            <MenuItem onClick={handleAddTransaction}>
              <ListItemIcon sx={{ color: "inherit" }}>
                <ReceiptLong fontSize="small" />
              </ListItemIcon>
              <ListItemText>Aggiungi transazione</ListItemText>
            </MenuItem>
            <MenuItem onClick={handleAddTransfer}>
              <ListItemIcon sx={{ color: "inherit" }}>
                <CompareArrows fontSize="small" />
              </ListItemIcon>
              <ListItemText>Aggiungi trasferimento</ListItemText>
            </MenuItem>
          </Menu>
          <Tooltip title="Aggiorna">
            <IconButton
              sx={{ ...controlBoxSx, color: controlLabelColor, px: 1.5 }}
              onClick={() => transactionsContext.refreshTransactions()}
            >
              <Refresh />
            </IconButton>
          </Tooltip>
        </Stack>
      </div>
      <TransferModal open={transferModalOpen} onClose={() => setTransferModalOpen(false)} />
    </>
  );
}
