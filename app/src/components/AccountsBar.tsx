import {
  Box,
  Checkbox,
  FormControl,
  ListItemText,
  MenuItem,
  Select,
  SelectChangeEvent,
  Stack,
  Typography,
} from "@mui/material";
import { KeyboardArrowLeft, KeyboardArrowRight } from "@mui/icons-material";
import { useAccounts } from "../stores/AccountContext";
import { useCategories } from "../stores/CategoryContext";
import { useTableContext } from "../stores/TableContext";

export default function AccountsBar() {
  const accountsContext = useAccounts();
  const categoriesContext = useCategories();
  const tableContext = useTableContext();
  const allAccountsOptionValue = "__all_accounts__";
  const allCategoriesOptionValue = "__all_categories__";

  const controlBoxSx = {
    height: 44,
    borderRadius: "10px",
    border: "1px solid rgba(255, 255, 255, 0.12)",
    backgroundColor: "rgba(255, 255, 255, 0.04)",
    display: "flex",
    alignItems: "center",
    px: 1.5,
  };

  const controlLabelColor = "#e5e7eb";

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

  const incrementMonth = () => {
    const newDate = new Date(tableContext.filterDate);
    newDate.setMonth(newDate.getMonth() + 1);
    tableContext.modifyFilterDate(newDate);
  };

  const decrementMonth = () => {
    const newDate = new Date(tableContext.filterDate);
    newDate.setMonth(newDate.getMonth() - 1);
    tableContext.modifyFilterDate(newDate);
  };

  const keyboardArrowStyle = {
    color: controlLabelColor,
    transition: "all 0.3s ease",
    "&:hover": {
      cursor: "pointer",
      color: "#111827",
      backgroundColor: "rgba(255, 255, 255, 0.8)",
      borderRadius: "50%",
      boxShadow: "0 6px 16px rgba(0, 0, 0, 0.25)",
    },
  };

  return (
    <>
      <div className="h-1/8 text-white flex flex-wrap items-center justify-start gap-3 px-2 py-3">
        <Box sx={{ ...controlBoxSx, minWidth: 300 }}>
          <FormControl size="small" sx={{ minWidth: 260, width: "100%" }}>
            <Select
              multiple
              value={tableContext.selectedAccountIds.map(String)}
              displayEmpty
              onChange={handleAccountChange}
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

        <Stack
          direction="row"
          spacing={1}
          sx={{ ...controlBoxSx, justifyContent: "center" }}
        >
          <KeyboardArrowLeft sx={keyboardArrowStyle} onClick={decrementMonth} />
          <Typography
            sx={{ color: controlLabelColor, fontWeight: 600, textTransform: "uppercase" }}
          >
            {tableContext.filterDate.toLocaleDateString("en-EN", {
              year: "numeric",
              month: "short",
            })}
          </Typography>
          <KeyboardArrowRight sx={keyboardArrowStyle} onClick={incrementMonth} />
        </Stack>

        <Stack
          direction="row"
          spacing={0.5}
          sx={{ ...controlBoxSx, justifyContent: "center" }}
        >
          <Checkbox
            onChange={() => tableContext.toggleIncludeMoneyTransfers()}
            checked={tableContext.includeMoneyTransfers}
            sx={{
              color: controlLabelColor,
              "&.Mui-checked": {
                color: controlLabelColor,
              },
            }}
          />
          <Typography sx={{ color: controlLabelColor, fontWeight: 500 }}>
            Include transfers
          </Typography>
        </Stack>
      </div>
    </>
  );
}
