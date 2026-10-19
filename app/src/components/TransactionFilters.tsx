import {
  Box,
  Button,
  Checkbox,
  Chip,
  FormControl,
  IconButton,
  ListItemText,
  MenuItem,
  Select,
  SelectChangeEvent,
  Stack,
  Tooltip,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import {
  AccountBalanceWalletOutlined,
  CategoryOutlined,
  ChevronLeftRounded,
  ChevronRightRounded,
  FilterAltOffOutlined,
  RefreshRounded,
} from "@mui/icons-material";
import { ReactNode } from "react";
import { useAccounts } from "../stores/AccountContext";
import { useCategories } from "../stores/CategoryContext";
import { useTableContext } from "../stores/TableContext";
import { useTransactions } from "../stores/TransactionContext";

const ALL = "__all__";

interface MultiSelectProps {
  icon: ReactNode;
  allLabel: string;
  options: { id: number; name: string }[];
  selected: number[];
  onChange: (ids: number[]) => void;
}

function MultiSelectFilter({ icon, allLabel, options, selected, onChange }: MultiSelectProps) {
  const a = useTheme().app;
  const isAll = selected.length === 0 || selected.length === options.length;

  const handleChange = (event: SelectChangeEvent<string[]>) => {
    const value = event.target.value;
    const values = typeof value === "string" ? value.split(",") : value;
    if (values.includes(ALL)) {
      onChange([]);
      return;
    }
    onChange(values.map(Number).filter(Number.isInteger));
  };

  return (
    <FormControl size="small" sx={{ minWidth: { xs: "100%", sm: 220 }, flex: { xs: "1 1 100%", sm: "0 1 260px" } }}>
      <Select
        multiple
        displayEmpty
        value={selected.map(String)}
        onChange={handleChange}
        startAdornment={<Box sx={{ display: "flex", color: a.textMuted, mr: 1, "& svg": { fontSize: 18 } }}>{icon}</Box>}
        renderValue={() => {
          if (isAll) return allLabel;
          const names = options.filter((o) => selected.includes(o.id)).map((o) => o.name);
          return names.length <= 2 ? names.join(", ") : `${names.length} selezionati`;
        }}
        sx={{ fontSize: "0.88rem" }}
      >
        <MenuItem value={ALL}>
          <Checkbox checked={isAll} />
          <ListItemText primary={allLabel} />
        </MenuItem>
        {options.map((option) => (
          <MenuItem key={option.id} value={String(option.id)}>
            <Checkbox checked={!isAll && selected.includes(option.id)} />
            <ListItemText primary={option.name} />
          </MenuItem>
        ))}
      </Select>
    </FormControl>
  );
}

/** Barra filtri dei movimenti: mese (con frecce), conti, categorie, chip dei filtri attivi. */
export default function TransactionFilters() {
  const a = useTheme().app;
  const { accounts } = useAccounts();
  const { allCategories } = useCategories();
  const table = useTableContext();
  const transactions = useTransactions();

  const months = table.availableMonths;
  const monthIndex = months.findIndex((m) => m.startDate === table.selectedMonth?.startDate);
  // i mesi arrivano dal più recente: "precedente" = indice successivo
  const goTo = (index: number) => {
    const month = months[index];
    if (month) table.modifySelectedMonth(month);
  };

  const activeAccounts = accounts.filter((x) => table.selectedAccountIds.includes(x.id));
  const activeCategories = allCategories.filter((x) => table.selectedCategoryIds.includes(x.id));
  const accountFilterOn = activeAccounts.length > 0 && activeAccounts.length < accounts.length;
  const categoryFilterOn = activeCategories.length > 0 && activeCategories.length < allCategories.length;
  const movementOn = table.movementType !== "all";
  const anyFilter = accountFilterOn || categoryFilterOn || movementOn;

  const resetFilters = () => {
    table.modifySelectedAccountIds([]);
    table.modifySelectedCategoryIds([]);
    if (movementOn) table.modifyMovementType(table.movementType);
  };

  return (
    <Stack spacing={1.5}>
      <Stack direction="row" spacing={1.25} useFlexGap flexWrap="wrap" alignItems="center">
        <Stack
          direction="row"
          alignItems="center"
          sx={{
            border: `1px solid ${a.borderStrong}`,
            borderRadius: `${a.radiusControl}px`,
            backgroundColor: a.surface,
            height: 40,
            flex: { xs: "1 1 100%", sm: "0 0 auto" },
          }}
        >
          <Tooltip title="Mese precedente">
            <span>
              <IconButton
                size="small"
                onClick={() => goTo(monthIndex + 1)}
                disabled={monthIndex < 0 || monthIndex >= months.length - 1}
                aria-label="Mese precedente"
              >
                <ChevronLeftRounded />
              </IconButton>
            </span>
          </Tooltip>
          <FormControl size="small" sx={{ flex: 1 }}>
            <Select
              variant="standard"
              disableUnderline
              displayEmpty
              value={table.selectedMonth?.startDate ?? ""}
              onChange={(e) => goTo(months.findIndex((m) => m.startDate === e.target.value))}
              disabled={table.availableMonthsLoading || months.length === 0}
              renderValue={() => table.selectedMonth?.description ?? "Nessun mese"}
              sx={{
                minWidth: 150,
                fontSize: "0.9rem",
                fontWeight: 650,
                textTransform: "capitalize",
                "& .MuiSelect-select": { py: 0.5, textAlign: "center", backgroundColor: "transparent !important" },
              }}
            >
              {months.map((m) => (
                <MenuItem key={m.startDate} value={m.startDate} sx={{ textTransform: "capitalize" }}>
                  {m.description}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <Tooltip title="Mese successivo">
            <span>
              <IconButton size="small" onClick={() => goTo(monthIndex - 1)} disabled={monthIndex <= 0} aria-label="Mese successivo">
                <ChevronRightRounded />
              </IconButton>
            </span>
          </Tooltip>
        </Stack>

        <MultiSelectFilter
          icon={<AccountBalanceWalletOutlined />}
          allLabel="Tutti i conti"
          options={accounts}
          selected={table.selectedAccountIds}
          onChange={table.modifySelectedAccountIds}
        />
        <MultiSelectFilter
          icon={<CategoryOutlined />}
          allLabel="Tutte le categorie"
          options={allCategories}
          selected={table.selectedCategoryIds}
          onChange={table.modifySelectedCategoryIds}
        />

        <Box sx={{ flex: 1 }} />
        <Tooltip title="Aggiorna">
          <IconButton
            onClick={() => transactions.refreshTransactions()}
            aria-label="Aggiorna"
            sx={{ border: `1px solid ${a.borderStrong}`, height: 40, width: 40, color: a.textSecondary }}
          >
            <RefreshRounded fontSize="small" />
          </IconButton>
        </Tooltip>
      </Stack>

      {anyFilter && (
        <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap" alignItems="center">
          {movementOn && (
            <Chip
              size="small"
              label={table.movementType === "incomes" ? "Solo entrate" : "Solo uscite"}
              onDelete={() => table.modifyMovementType(table.movementType)}
            />
          )}
          {accountFilterOn &&
            activeAccounts.map((x) => (
              <Chip
                key={`a-${x.id}`}
                size="small"
                label={x.name}
                icon={<AccountBalanceWalletOutlined />}
                onDelete={() => table.modifySelectedAccountIds(table.selectedAccountIds.filter((id) => id !== x.id))}
              />
            ))}
          {categoryFilterOn &&
            activeCategories.map((x) => (
              <Chip
                key={`c-${x.id}`}
                size="small"
                label={x.name}
                icon={<CategoryOutlined />}
                onDelete={() => table.modifySelectedCategoryIds(table.selectedCategoryIds.filter((id) => id !== x.id))}
              />
            ))}
          <Button size="small" startIcon={<FilterAltOffOutlined />} onClick={resetFilters}>
            Azzera filtri
          </Button>
        </Stack>
      )}
    </Stack>
  );
}
