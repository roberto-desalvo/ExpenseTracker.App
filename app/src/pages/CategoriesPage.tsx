import { useMemo, useState } from "react";
import {
  Autocomplete,
  Box,
  Button,
  Chip,
  IconButton,
  Stack,
  TableCell,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { AddRounded, DeleteOutlineRounded, EditOutlined, LockOutlined } from "@mui/icons-material";
import Category from "../models/Category";
import { useCategories } from "../stores/CategoryContext";
import { useSuccessMessage } from "../stores/SuccessMessageContext";
import DataTableBase, { DataTableColumn } from "../components/DataTableBase";
import ConfirmDeleteDialog from "../components/ConfirmDeleteDialog";
import EditorDrawer from "../components/ui/EditorDrawer";
import SearchField from "../components/ui/SearchField";
import { initialsOf } from "../utilities/format.utilities";

type CategoryFormState = {
  name: string;
  description: string;
  priority: string;
  tags: string[];
};

const createInitialForm = (): CategoryFormState => ({
  name: "",
  description: "",
  priority: "0",
  tags: [],
});

const columns: DataTableColumn[] = [
  { id: "name", label: "Categoria", minWidth: "36%" },
  { id: "tags", label: "Parole chiave import", minWidth: "40%" },
  { id: "priority", label: "Priorità", minWidth: "10%", align: "right" },
  { id: "actions", label: "", minWidth: "14%", align: "right" },
];

export default function CategoriesPage() {
  const a = useTheme().app;
  const { showSuccess } = useSuccessMessage();
  const {
    categories,
    allCategories,
    isLoading,
    page,
    pageSize,
    totalCount,
    modifyPage,
    modifyPageSize,
    addCategory,
    updateCategory,
    deleteCategory,
    refreshCategories,
  } = useCategories();

  const [form, setForm] = useState<CategoryFormState>(createInitialForm());
  const [editorOpen, setEditorOpen] = useState(false);
  const [editing, setEditing] = useState<Category | null>(null);
  const [toDelete, setToDelete] = useState<Category | null>(null);
  const [busy, setBusy] = useState(false);

  const knownTags = useMemo(
    () => [...new Set(allCategories.flatMap((c) => c.tags ?? []))].sort((x, y) => x.localeCompare(y)),
    [allCategories],
  );

  const openCreate = () => {
    setEditing(null);
    setForm(createInitialForm());
    setEditorOpen(true);
  };

  const openEdit = (category: Category) => {
    setEditing(category);
    setForm({
      name: category.name,
      description: category.description ?? "",
      priority: String(category.priority ?? 0),
      tags: category.tags ?? [],
    });
    setEditorOpen(true);
  };

  const handleSubmit = async () => {
    const payload: Category = {
      id: editing?.id ?? 0,
      name: form.name.trim(),
      description: form.description.trim(),
      priority: Number(form.priority) || 0,
      isDefault: editing?.isDefault ?? false,
      tags: form.tags.map((t) => t.trim()).filter(Boolean),
    };

    if (!payload.name) return;

    setBusy(true);
    try {
      if (payload.id > 0) {
        await updateCategory(payload);
        showSuccess(`Categoria "${payload.name}" modificata`);
      } else {
        await addCategory(payload);
        showSuccess(`Categoria "${payload.name}" creata`);
      }
      setEditorOpen(false);
    } catch {
      // errore mostrato dal layer API
    } finally {
      setBusy(false);
    }
  };

  const handleDelete = async (): Promise<string | void> => {
    if (!toDelete) return;
    setBusy(true);
    try {
      const name = toDelete.name;
      await deleteCategory(toDelete.id);
      setToDelete(null);
      setEditorOpen(false);
      return `Categoria "${name}" eliminata`;
    } finally {
      setBusy(false);
    }
  };

  const renderRow = (category: Category) => (
    <TableRow
      key={category.id}
      hover
      onClick={() => openEdit(category)}
      sx={{
        cursor: "pointer",
        "&.MuiTableRow-hover:hover": { backgroundColor: a.surfaceAlt },
        "&:hover .row-actions, &:focus-within .row-actions": { opacity: 1 },
      }}
    >
      <TableCell>
        <Stack direction="row" spacing={1.5} alignItems="center">
          <Box
            sx={{
              width: 34,
              height: 34,
              flexShrink: 0,
              borderRadius: `${Math.max(a.radiusControl, 6)}px`,
              display: "grid",
              placeItems: "center",
              backgroundColor: category.isDefault ? a.surfaceAlt : a.primarySoft,
              color: category.isDefault ? a.textMuted : a.primary,
              fontWeight: 700,
              fontSize: "0.76rem",
            }}
          >
            {initialsOf(category.name)}
          </Box>
          <Box sx={{ minWidth: 0 }}>
            <Stack direction="row" spacing={1} alignItems="center">
              <Typography sx={{ fontWeight: 600, fontSize: "0.88rem" }} noWrap>
                {category.name}
              </Typography>
              {category.isDefault && (
                <Chip size="small" icon={<LockOutlined sx={{ fontSize: "14px !important" }} />} label="Predefinita" sx={{ height: 22, fontSize: "0.7rem" }} />
              )}
            </Stack>
            <Typography sx={{ fontSize: "0.78rem", color: a.textMuted }} noWrap>
              {category.description || "Nessuna descrizione"}
            </Typography>
          </Box>
        </Stack>
      </TableCell>
      <TableCell>
        {category.tags.length > 0 ? (
          <Stack direction="row" spacing={0.5} useFlexGap flexWrap="wrap">
            {category.tags.slice(0, 5).map((tag) => (
              <Chip key={tag} label={tag} size="small" variant="outlined" sx={{ height: 22, fontSize: "0.72rem", borderColor: a.border }} />
            ))}
            {category.tags.length > 5 && (
              <Chip label={`+${category.tags.length - 5}`} size="small" sx={{ height: 22, fontSize: "0.72rem" }} />
            )}
          </Stack>
        ) : (
          <Typography sx={{ fontSize: "0.8rem", color: a.textMuted }}>—</Typography>
        )}
      </TableCell>
      <TableCell align="right">
        <Typography sx={{ fontSize: "0.86rem", fontWeight: 600, fontVariantNumeric: "tabular-nums" }}>{category.priority}</Typography>
      </TableCell>
      <TableCell align="right" onClick={(e) => e.stopPropagation()}>
        <Stack className="row-actions" direction="row" spacing={0.25} justifyContent="flex-end" sx={{ opacity: { xs: 1, md: 0 }, transition: "opacity .15s" }}>
          <Tooltip title="Modifica">
            <IconButton size="small" onClick={() => openEdit(category)} aria-label={`Modifica ${category.name}`} sx={{ color: a.textSecondary }}>
              <EditOutlined fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title={category.isDefault ? "La categoria predefinita non si può eliminare" : "Elimina"}>
            <span>
              <IconButton
                size="small"
                disabled={category.isDefault}
                onClick={() => setToDelete(category)}
                aria-label={`Elimina ${category.name}`}
                sx={{ color: a.textSecondary, "&:hover": { color: a.expense } }}
              >
                <DeleteOutlineRounded fontSize="small" />
              </IconButton>
            </span>
          </Tooltip>
        </Stack>
      </TableCell>
    </TableRow>
  );

  return (
    <Stack spacing={2}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} alignItems={{ xs: "stretch", sm: "flex-start" }}>
        <SearchField
          placeholder="Cerca categoria per nome"
          onSearch={(name) => {
            modifyPage(0);
            void refreshCategories(name);
          }}
        />
        <Box sx={{ flex: 1 }} />
        <Button variant="contained" startIcon={<AddRounded />} onClick={openCreate}>
          Nuova categoria
        </Button>
      </Stack>

      <DataTableBase
        columns={columns}
        rows={categories}
        isLoading={isLoading}
        isEmpty={!isLoading && categories.length === 0}
        emptyMessage="Nessuna categoria trovata"
        emptySubtext="Crea una categoria per organizzare i movimenti e classificarli in automatico durante l'import."
        page={page}
        pageSize={pageSize}
        totalCount={totalCount}
        onPageChange={(_e, newPage) => modifyPage(newPage)}
        onPageSizeChange={(e) => modifyPageSize(parseInt(e.target.value, 10))}
        minTableWidth={640}
        renderRow={renderRow}
      />

      <EditorDrawer
        open={editorOpen}
        onClose={() => setEditorOpen(false)}
        title={editing ? "Modifica categoria" : "Nuova categoria"}
        subtitle={editing?.isDefault ? "Categoria predefinita: riceve i movimenti non classificati" : undefined}
        onSubmit={() => void handleSubmit()}
        isBusy={busy}
        submitDisabled={form.name.trim().length === 0}
        submitLabel={editing ? "Salva modifiche" : "Crea categoria"}
        secondaryAction={
          editing && !editing.isDefault ? (
            <Button color="error" startIcon={<DeleteOutlineRounded />} onClick={() => setToDelete(editing)} disabled={busy}>
              Elimina
            </Button>
          ) : undefined
        }
      >
        <TextField
          label="Nome"
          required
          autoFocus
          value={form.name}
          onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
          disabled={busy}
        />
        <TextField
          label="Descrizione"
          multiline
          minRows={2}
          value={form.description}
          onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
          disabled={busy}
        />
        <Autocomplete
          multiple
          freeSolo
          options={knownTags}
          value={form.tags}
          onChange={(_e, value) => setForm((f) => ({ ...f, tags: value.map(String) }))}
          disabled={busy}
          renderTags={(value, getTagProps) =>
            value.map((option, index) => {
              const { key, ...tagProps } = getTagProps({ index });
              return <Chip key={key} label={option} size="small" {...tagProps} />;
            })
          }
          renderInput={(params) => (
            <TextField
              {...params}
              label="Parole chiave"
              placeholder={form.tags.length ? "" : "Es. esselunga, conad"}
              helperText="Premi Invio dopo ogni parola. Durante l'import, un movimento la cui descrizione contiene una di queste parole viene assegnato a questa categoria."
            />
          )}
        />
        <TextField
          label="Priorità"
          type="number"
          value={form.priority}
          onChange={(e) => setForm((f) => ({ ...f, priority: e.target.value }))}
          disabled={busy}
          helperText="Se più categorie corrispondono, vince quella con il numero più basso."
          sx={{ maxWidth: 200 }}
        />
      </EditorDrawer>

      <ConfirmDeleteDialog
        open={toDelete !== null}
        onClose={() => setToDelete(null)}
        onConfirm={handleDelete}
        isBusy={busy}
        title="Eliminare la categoria?"
        message={
          <>
            I movimenti collegati a &ldquo;{toDelete?.name}&rdquo; verranno riassegnati alla categoria predefinita.
          </>
        }
      />
    </Stack>
  );
}
