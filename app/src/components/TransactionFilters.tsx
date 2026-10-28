import {
  Box,
  Button,
  Checkbox,
  FormControl,
  IconButton,
  ListItemText,
  ListSubheader,
  MenuItem,
  Select,
  SelectChangeEvent,
  Stack,
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
  Typography,
} from "@mui/material";
import { DatePicker, LocalizationProvider } from "@mui/x-date-pickers";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import dayjs from "dayjs";
import "dayjs/locale/it";
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
import { MovementType, PeriodMode, useTableContext } from "../stores/TableContext";
import { useTransactions } from "../stores/TransactionContext";

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
  // nessun filtro = tutte le opzioni spuntate: un click su una voce la toglie dalla selezione
  const checkedIds = isAll ? options.map((o) => o.id) : selected;

  // selezionare tutto o togliere l'ultima voce equivale a nessun filtro
  const handleChange = (event: SelectChangeEvent<string[]>) => {
    const value = event.target.value;
    const ids = (typeof value === "string" ? value.split(",") : value).map(Number).filter(Number.isInteger);
    onChange(ids.length === options.length ? [] : ids);
  };

  return (
    <FormControl size="small" sx={{ minWidth: 0, flex: { xs: "1 1 100%", sm: "1 1 0" } }}>
      <Select
        multiple
        displayEmpty
        value={checkedIds.map(String)}
        onChange={handleChange}
        startAdornment={<Box sx={{ display: "flex", color: a.textMuted, mr: 1, "& svg": { fontSize: 18 } }}>{icon}</Box>}
        renderValue={() => {
          if (isAll) return allLabel;
          const names = options.filter((o) => selected.includes(o.id)).map((o) => o.name);
          return names.length <= 2 ? names.join(", ") : `${names.length} selezionati`;
        }}
        sx={{ fontSize: "0.88rem" }}
      >
        <ListSubheader
          sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", lineHeight: "40px", backgroundColor: a.surface }}
        >
          {isAll ? allLabel : `${selected.length} di ${options.length}`}
          <Button size="small" disabled={isAll} onClick={() => onChange([])}>
            Seleziona tutti
          </Button>
        </ListSubheader>
        {options.map((option) => (
          <MenuItem key={option.id} value={String(option.id)}>
            <Checkbox checked={checkedIds.includes(option.id)} />
            <ListItemText primary={option.name} />
          </MenuItem>
        ))}
      </Select>
    </FormControl>
  );
}

const toggleGroupSx = { height: 40, flex: { xs: "1 1 100%", sm: "0 0 auto" }, "& .MuiToggleButton-root": { flex: 1, px: 1.5 } };

/**
 * Barra filtri dei movimenti: periodo (mese intero con frecce oppure intervallo di date personalizzato),
 * tipo di movimento, conti e categorie.
 */
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

  const { from, to } = table.customRange;
  const fromValue = from ? dayjs(from) : null;
  const toValue = to ? dayjs(to) : null;

  const accountFilterOn = table.selectedAccountIds.length > 0 && table.selectedAccountIds.length < accounts.length;
  const categoryFilterOn = table.selectedCategoryIds.length > 0 && table.selectedCategoryIds.length < allCategories.length;
  const anyFilter = accountFilterOn || categoryFilterOn || table.movementType !== "all";

  const resetFilters = () => {
    table.modifySelectedAccountIds([]);
    table.modifySelectedCategoryIds([]);
    table.modifyMovementType("all");
  };

  // contenitore comune di selettore mese e intervallo date: stessa altezza e bordo degli altri controlli
  const periodBoxSx = {
    border: `1px solid ${a.borderStrong}`,
    borderRadius: `${a.radiusControl}px`,
    backgroundColor: a.surface,
    height: 40,
    flex: { xs: "1 1 100%", sm: "0 0 auto" },
  };

  // date picker senza bordo né etichetta flottante, da affiancare dentro periodBoxSx;
  // il campo si dimensiona sul testo (gg/mm/aaaa) e l'icona calendario non usa il margine negativo di edge="end"
  const rangePickerSx = {
    flex: { xs: 1, sm: "0 0 auto" },
    "& .MuiInputBase-root:before, & .MuiInputBase-root:after": { display: "none" },
    "& .MuiInputBase-input": { fontSize: "0.9rem", fontWeight: 650, py: 0.5, width: { xs: "100%", sm: "11ch" } },
    "& .MuiInputAdornment-root": { ml: 0.25 },
  };
  const rangePickerSlotProps = (ariaLabel: string) => ({
    textField: { variant: "standard" as const, size: "small" as const, inputProps: { "aria-label": ariaLabel } },
    openPickerButton: { size: "small" as const, edge: false as const, sx: { p: 0.5, color: a.textMuted } },
    openPickerIcon: { sx: { fontSize: 18 } },
  });

  const rangeLabel = (text: string) => (
    <Typography component="span" sx={{ fontSize: "0.78rem", fontWeight: 600, color: a.textMuted }}>
      {text}
    </Typography>
  );

  return (
    <Stack spacing={1.25}>
      <Stack direction="row" spacing={1.25} useFlexGap flexWrap="wrap" alignItems="center">
        <ToggleButtonGroup
          size="small"
          exclusive
          value={table.periodMode}
          onChange={(_e, next: PeriodMode | null) => next && table.modifyPeriodMode(next)}
          aria-label="Periodo"
          sx={toggleGroupSx}
        >
          <ToggleButton value="month">Mese</ToggleButton>
          <ToggleButton value="custom">Personalizzato</ToggleButton>
        </ToggleButtonGroup>

        {table.periodMode === "month" ? (
          <Stack direction="row" alignItems="center" sx={periodBoxSx}>
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
        ) : (
          <LocalizationProvider dateAdapter={AdapterDayjs} adapterLocale="it">
            <Stack direction="row" alignItems="center" spacing={1} sx={{ ...periodBoxSx, pl: 1.5, pr: 0.75 }}>
              {rangeLabel("Dal")}
              <DatePicker
                value={fromValue}
                maxDate={toValue ?? undefined}
                onChange={(value) => table.modifyCustomRange({ from: value?.isValid() ? value.format("YYYY-MM-DD") : null, to })}
                slotProps={rangePickerSlotProps("Dal")}
                sx={rangePickerSx}
              />
              <Box sx={{ width: "1px", alignSelf: "stretch", my: 1, backgroundColor: a.borderStrong }} />
              {rangeLabel("Al")}
              <DatePicker
                value={toValue}
                minDate={fromValue ?? undefined}
                onChange={(value) => table.modifyCustomRange({ from, to: value?.isValid() ? value.format("YYYY-MM-DD") : null })}
                slotProps={rangePickerSlotProps("Al")}
                sx={rangePickerSx}
              />
            </Stack>
          </LocalizationProvider>
        )}

        <Box sx={{ flex: 1, display: { xs: "none", md: "block" } }} />
        <ToggleButtonGroup
          size="small"
          exclusive
          value={table.movementType}
          onChange={(_e, next: MovementType | null) => next && table.modifyMovementType(next)}
          aria-label="Tipo di movimento"
          sx={toggleGroupSx}
        >
          <ToggleButton value="all">Tutti</ToggleButton>
          <ToggleButton value="incomes">Entrate</ToggleButton>
          <ToggleButton value="outcomes">Uscite</ToggleButton>
        </ToggleButtonGroup>
      </Stack>

      <Stack direction="row" spacing={1.25} useFlexGap flexWrap={{ xs: "wrap", sm: "nowrap" }} alignItems="center">
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

        <Box sx={{ flex: { xs: 1, sm: "0 0 auto" } }} />
        {anyFilter && (
          <Button size="small" startIcon={<FilterAltOffOutlined />} onClick={resetFilters} sx={{ flexShrink: 0, whiteSpace: "nowrap" }}>
            Azzera filtri
          </Button>
        )}
        <Tooltip title="Aggiorna">
          <IconButton
            onClick={() => transactions.refreshTransactions()}
            aria-label="Aggiorna"
            sx={{ border: `1px solid ${a.borderStrong}`, height: 40, width: 40, flexShrink: 0, color: a.textSecondary }}
          >
            <RefreshRounded fontSize="small" />
          </IconButton>
        </Tooltip>
      </Stack>
    </Stack>
  );
}
