import { useState } from "react";
import {
  Chip,
  Stack,
  TextField,
  Typography,
  TableRow,
  TableCell,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { DeleteOutlineRounded, EditRounded } from "@mui/icons-material";
import Category from "../models/Category";
import { useCategories } from "../stores/CategoryContext";
import DataTableBase, { DataTableColumn } from "../components/DataTableBase";
import RowActionsMenu from "../components/RowActionsMenu";
import CategoriesFilterBar from "../components/CategoriesFilterBar";
import AppModal from "../components/AppModal";
import ConfirmDeleteDialog from "../components/ConfirmDeleteDialog";

type CategoryFormState = {
  name: string;
  description: string;
  priority: string;
  tags: string;
};

const createInitialForm = (): CategoryFormState => ({
  name: "",
  description: "",
  priority: "0",
  tags: "",
});

const parseTags = (tags: string): string[] =>
  tags
    .split(/[,;]/)
    .map((tag) => tag.trim())
    .filter((tag) => tag.length > 0);

const formatTags = (tags: string[]): string => tags.join(", ");

export default function CategoriesPage() {
  const theme = useTheme();
  const c = theme.palette.custom;
  const {
    categories,
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
  const [modalOpen, setModalOpen] = useState<boolean>(false);
  const [editingCategory, setEditingCategory] = useState<Category | null>(null);
  const [selectedCategory, setSelectedCategory] = useState<Category | null>(
    null,
  );
  const [deleteDialogOpen, setDeleteDialogOpen] = useState<boolean>(false);
  const [operationInProgress, setOperationInProgress] =
    useState<boolean>(false);

  const columns: DataTableColumn[] = [
    { id: "name", label: "Nome", minWidth: 150 },
    { id: "description", label: "Descrizione", minWidth: 200 },
    { id: "tags", label: "Tag", minWidth: 150 },
    { id: "priority", label: "Priorità", minWidth: 80, align: "right" },
    { id: "actions", label: "Azioni", minWidth: 80, align: "center" },
  ];

  const handleSearch = (name?: string) => {
    modifyPage(0);
    void refreshCategories(name);
  };

  const openCreateModal = () => {
    setEditingCategory(null);
    setForm(createInitialForm());
    setModalOpen(true);
  };

  const openEditModal = (category: Category) => {
    setEditingCategory(category);
    setForm({
      name: category.name,
      description: category.description ?? "",
      priority: String(category.priority ?? 0),
      tags: formatTags(category.tags ?? []),
    });
    setModalOpen(true);
  };

  const closeModal = () => {
    setModalOpen(false);
  };

  const handleDeleteRequest = (category: Category) => {
    if (category.isDefault) return;
    setSelectedCategory(category);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async (): Promise<string | void> => {
    if (!selectedCategory) {
      return;
    }

    setOperationInProgress(true);
    try {
      const name = selectedCategory.name;
      await deleteCategory(selectedCategory.id);
      setSelectedCategory(null);
      return `Categoria "${name}" eliminata`;
    } finally {
      setOperationInProgress(false);
    }
  };

  const handleDeleteCancel = () => {
    setDeleteDialogOpen(false);
  };

  const handleSubmit = async (event: React.FormEvent): Promise<string | void> => {
    event.preventDefault();

    const payload: Category = {
      id: editingCategory?.id ?? 0,
      name: form.name.trim(),
      description: form.description.trim(),
      priority: Number(form.priority) || 0,
      isDefault: editingCategory?.isDefault ?? false,
      tags: parseTags(form.tags),
    };

    if (payload.name.length === 0) {
      return;
    }

    setOperationInProgress(true);
    try {
      if (payload.id > 0) {
        await updateCategory(payload);
        return `Categoria "${payload.name}" modificata`;
      } else {
        await addCategory(payload);
        return `Categoria "${payload.name}" creata`;
      }
    } finally {
      setOperationInProgress(false);
    }
  };

  const renderCategoryRow = (category: Category) => (
    <TableRow
      hover
      key={category.id}
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
            {category.name}
          </Typography>
          {category.isDefault && (
            <Chip
              label="Default"
              size="small"
              sx={{
                backgroundColor: c.badgeBackground,
                border: `1px solid ${c.badgeBorder}`,
                color: c.badgeText,
                fontWeight: 600,
              }}
            />
          )}
        </Stack>
      </TableCell>
      <TableCell
        sx={{
          borderBottom: "none",
          backgroundColor: c.rowBackground,
          color: "text.secondary",
          py: 1.5,
        }}
      >
        {category.description || "-"}
      </TableCell>
      <TableCell
        sx={{
          borderBottom: "none",
          backgroundColor: c.rowBackground,
          color: "text.secondary",
          py: 1.5,
        }}
      >
        {category.tags.length > 0 ? category.tags.join(", ") : "-"}
      </TableCell>
      <TableCell
        align="right"
        sx={{
          borderBottom: "none",
          backgroundColor: c.rowBackground,
          color: "text.primary",
          py: 1.5,
        }}
      >
        {category.priority}
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
          rowId={category.id}
          ariaLabel="Azioni categoria"
          actions={[
            {
              label: "Modifica",
              icon: <EditRounded fontSize="small" />,
              onClick: () => openEditModal(category),
            },
            {
              label: "Elimina",
              icon: <DeleteOutlineRounded fontSize="small" />,
              onClick: () => handleDeleteRequest(category),
              disabled: category.isDefault,
            },
          ]}
        />
      </TableCell>
    </TableRow>
  );

  return (
    <>
      <CategoriesFilterBar
        onSearch={handleSearch}
        onAddClick={openCreateModal}
        onRefresh={() => void refreshCategories()}
        isLoading={isLoading}
      />
      <main className="flex-1 min-h-0 px-2 pb-2">
      <DataTableBase
        title="Categorie"
        columns={columns}
        rows={categories}
        isLoading={isLoading}
        isEmpty={!isLoading && categories.length === 0}
        emptyMessage="Nessuna categoria trovata"
        emptySubtext="Crea la tua prima categoria per iniziare"
        page={page}
        pageSize={pageSize}
        totalCount={totalCount}
        onPageChange={(_event, newPage) => modifyPage(newPage)}
        onPageSizeChange={(event) =>
          modifyPageSize(parseInt(event.target.value, 10))
        }
        renderRow={(category) => renderCategoryRow(category)}
      />

      <ConfirmDeleteDialog
        open={deleteDialogOpen}
        onClose={handleDeleteCancel}
        onConfirm={handleDeleteConfirm}
        isBusy={operationInProgress}
        message={
          <>Vuoi eliminare la categoria &ldquo;{selectedCategory?.name}&rdquo;? Le transazioni collegate verranno riassegnate alla categoria di default.</>
        }
      />

      <AppModal
        open={modalOpen}
        onClose={closeModal}
        title={editingCategory ? "Modifica categoria" : "Nuova categoria"}
        onSubmit={handleSubmit}
        isBusy={operationInProgress}
        submitLabel={editingCategory ? "Salva modifiche" : "Crea categoria"}
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
        <TextField
          label="Descrizione"
          value={form.description}
          onChange={(event) =>
            setForm((prev) => ({
              ...prev,
              description: event.target.value,
            }))
          }
          fullWidth
          disabled={operationInProgress}
        />
        <TextField
          label="Tag (separati da virgola)"
          value={form.tags}
          onChange={(event) =>
            setForm((prev) => ({ ...prev, tags: event.target.value }))
          }
          fullWidth
          disabled={operationInProgress}
        />
        <TextField
          label="Priorità"
          type="number"
          value={form.priority}
          onChange={(event) =>
            setForm((prev) => ({ ...prev, priority: event.target.value }))
          }
          fullWidth
          disabled={operationInProgress}
        />
      </AppModal>

      </main>
    </>
  );
}
