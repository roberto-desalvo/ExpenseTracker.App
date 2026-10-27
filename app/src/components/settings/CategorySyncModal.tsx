import { useEffect, useState } from "react";
import { Alert, Stack, TextField, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import AppModal from "../AppModal";
import CategoryService from "../../services/CategoryService";
import { toDateInputValue } from "../../utilities/date.utilities";

interface CategorySyncModalProps {
  open: boolean;
  onClose: () => void;
}

const createInitialRange = () => {
  const today = new Date();
  return {
    start: toDateInputValue(new Date(today.getFullYear(), 0, 1)),
    end: toDateInputValue(today),
  };
};

/** Riapplica le parole chiave delle categorie ai movimenti già salvati in un periodo. */
export default function CategorySyncModal({ open, onClose }: CategorySyncModalProps) {
  const a = useTheme().app;
  const [range, setRange] = useState(createInitialRange);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (open) setRange(createInitialRange());
  }, [open]);

  const invalidRange = !range.start || !range.end || range.start > range.end;

  const handleSubmit = async (): Promise<string> => {
    if (invalidRange) throw new Error("Periodo non valido");
    setBusy(true);
    try {
      // date yyyy-MM-dd senza orario: il backend considera solo il giorno, nessuno slittamento di fuso
      const result = await CategoryService.syncTransactions({ startDate: range.start, endDate: range.end });
      return result.updatedTransactions === 0
        ? `Nessun movimento da aggiornare (${result.examinedTransactions} analizzati)`
        : `${result.updatedTransactions} movimenti riclassificati su ${result.examinedTransactions} analizzati`;
    } finally {
      setBusy(false);
    }
  };

  return (
    <AppModal
      open={open}
      onClose={onClose}
      title="Sincronizza categorie"
      subtitle="Riassegna la categoria ai movimenti del periodo in base alle parole chiave"
      onSubmit={handleSubmit}
      isBusy={busy}
      submitLabel="Sincronizza"
    >
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
        <TextField
          type="date"
          label="Dal"
          required
          fullWidth
          value={range.start}
          onChange={(e) => setRange((r) => ({ ...r, start: e.target.value }))}
          disabled={busy}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField
          type="date"
          label="Al"
          required
          fullWidth
          value={range.end}
          onChange={(e) => setRange((r) => ({ ...r, end: e.target.value }))}
          disabled={busy}
          slotProps={{ inputLabel: { shrink: true } }}
        />
      </Stack>
      {range.start && range.end && range.start > range.end && (
        <Alert severity="warning">La data di inizio deve precedere la data di fine.</Alert>
      )}
      <Typography sx={{ fontSize: "0.82rem", color: a.textSecondary }}>
        Un movimento la cui descrizione contiene una parola chiave passa alla categoria corrispondente; se ne
        contiene di più categorie vince quella con priorità più bassa. I trasferimenti tra conti non vengono
        modificati e i movimenti senza corrispondenze mantengono la categoria attuale.
      </Typography>
    </AppModal>
  );
}
