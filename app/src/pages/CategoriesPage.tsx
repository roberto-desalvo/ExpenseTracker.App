import { useMemo, useState, useEffect, type MouseEvent } from "react";
import {
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  IconButton,
  InputAdornment,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Modal,
  Paper,
  Skeleton,
  Snackbar,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
  Alert,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import {
  Add,
  DeleteOutlineRounded,
  EditRounded,
  MoreVertRounded,
  Search,
} from "@mui/icons-material";
import Category from "../models/Category";
import { useCategories } from "../stores/CategoryContext";

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
    addCategory,
    updateCategory,
    deleteCategory,
    refreshCategories,
  } = useCategories();

  const [searchTerm, setSearchTerm] = useState<string>("");
  const [form, setForm] = useState<CategoryFormState>(createInitialForm());
  const [modalOpen, setModalOpen] = useState<boolean>(false);
  const [editingCategory, setEditingCategory] = useState<Category | null>(null);
  const [menuAnchorEl, setMenuAnchorEl] = useState<null | HTMLElement>(null);
  const [selectedCategory, setSelectedCategory] = useState<Category | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState<boolean>(false);
  const [operationInProgress, setOperationInProgress] = useState<boolean>(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [successOpen, setSuccessOpen] = useState<boolean>(false);

  const isActionsMenuOpen = Boolean(menuAnchorEl);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      const normalized = searchTerm.trim();

      if (normalized.length >= 3) {
        void refreshCategories(normalized);
        return;
      }

      void refreshCategories();
    }, 350);

    return () => {
      clearTimeout(timeoutId);
    };
  }, [searchTerm]);

  const searchHint = useMemo(() => {
    const normalized = searchTerm.trim();

    if (normalized.length === 0) {
      return "Ricerca live per nome (minimo 3 caratteri).";
    }

    if (normalized.length < 3) {
      return "Digita almeno 3 caratteri per filtrare per nome.";
    }

    return `Risultati per "${normalized}"`;
  }, [searchTerm]);

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

  const openActionsMenu = (
    event: MouseEvent<HTMLElement>,
    category: Category
  ) => {
    setMenuAnchorEl(event.currentTarget);
    setSelectedCategory(category);
  };

  const closeActionsMenu = () => {
    setMenuAnchorEl(null);
  };

  const handleDeleteRequest = () => {
    if (!selectedCategory || selectedCategory.isDefault) {
      closeActionsMenu();
      return;
    }

    setDeleteDialogOpen(true);
    closeActionsMenu();
  };

  const handleDeleteConfirm = async () => {
    if (!selectedCategory) {
      return;
    }

    setOperationInProgress(true);
    try {
      await deleteCategory(selectedCategory.id);
      setSuccessMessage(`Categoria "${selectedCategory.name}" eliminata`);
      setSuccessOpen(true);
      setDeleteDialogOpen(false);
      setSelectedCategory(null);
    } finally {
      setOperationInProgress(false);
    }
  };

  const handleDeleteCancel = () => {
    setDeleteDialogOpen(false);
  };

  const handleEditRequest = () => {
    if (!selectedCategory) {
      return;
    }

    openEditModal(selectedCategory);
    closeActionsMenu();
  };

  const handleSubmit = async (event: React.FormEvent) => {
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
        setSuccessMessage(`Categoria "${payload.name}" modificata`);
      } else {
        await addCategory(payload);
        setSuccessMessage(`Categoria "${payload.name}" creata`);
      }
      setSuccessOpen(true);
      closeModal();

      const normalized = searchTerm.trim();
      if (normalized.length >= 3) {
        await refreshCategories(normalized);
        return;
      }

      await refreshCategories();
    } finally {
      setOperationInProgress(false);
    }
  };

  return (
    <main className="flex-1 min-h-0 px-2 pb-2">
      <Paper
        sx={{
          height: "100%",
          width: "100%",
          overflow: "hidden",
          background: "transparent",
          display: "flex",
          flexDirection: "column",
          borderRadius: 4,
          border: `1px solid ${theme.palette.divider}`,
        }}
      >
        <Box
          sx={{
            px: { xs: 2, md: 3 },
            pt: { xs: 2, md: 2 },
            pb: 1.5,
            borderBottom: `1px solid ${theme.palette.divider}`,
          }}
        >
          <Stack
            direction={{ xs: "column", md: "row" }}
            spacing={1.5}
            alignItems={{ xs: "stretch", md: "center" }}
            justifyContent="space-between"
          >
            <Stack spacing={0.5}>
              <Typography
                variant="h6"
                sx={{
                  color: "text.primary",
                  fontWeight: 600,
                  letterSpacing: "-0.01em",
                  fontSize: "1rem",
                }}
              >
                Categorie
              </Typography>
              <Typography variant="caption" sx={{ color: "text.secondary" }}>
                {searchHint}
              </Typography>
            </Stack>
            <Stack direction={{ xs: "column", sm: "row" }} spacing={1.25}>
              <TextField
                value={searchTerm}
                onChange={(event) => setSearchTerm(event.target.value)}
                placeholder="Filtra per nome"
                size="small"
                sx={{ minWidth: { xs: 240, md: 300 } }}
                slotProps={{
                  input: {
                    startAdornment: (
                      <InputAdornment position="start">
                        <Search fontSize="small" />
                      </InputAdornment>
                    ),
                  },
                }}
              />
              <Button
                variant="contained"
                startIcon={<Add />}
                onClick={openCreateModal}
                disabled={isLoading || operationInProgress}
                sx={{
                  backgroundColor: c.accentColor,
                  color: theme.palette.mode === "dark" ? "#0f172a" : "#ffffff",
                  "&:hover": {
                    backgroundColor:
                      theme.palette.mode === "dark" ? "#bef264" : "#4d7c0f",
                  },
                }}
              >
                Nuova categoria
              </Button>
            </Stack>
          </Stack>
        </Box>

        <TableContainer
          sx={{
            flex: 1,
            minHeight: 0,
            overflow: "auto",
            px: { xs: 1.5, md: 2 },
            py: 1.5,
          }}
        >
          {isLoading ? (
            <Box sx={{ p: 3 }}>
              <Stack spacing={1}>
                {[...Array(5)].map((_, i) => (
                  <Skeleton key={i} variant="rectangular" height={40} />
                ))}
              </Stack>
            </Box>
          ) : categories.length === 0 ? (
            <Box
              sx={{
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                height: "100%",
                minHeight: 300,
                flexDirection: "column",
                gap: 2,
              }}
            >
              <Typography variant="h6" sx={{ color: "text.secondary" }}>
                Nessuna categoria trovata
              </Typography>
              <Typography variant="body2" sx={{ color: "text.secondary" }}>
                Crea la tua prima categoria per iniziare
              </Typography>
            </Box>
          ) : (
            <Table
              stickyHeader
              sx={{
                borderCollapse: "separate",
                borderSpacing: "0 10px",
                minWidth: 760,
              }}
            >
              <TableHead>
                <TableRow>
                  <TableCell
                    sx={{
                      background: "background.default",
                      color: c.tableHeaderText,
                      textTransform: "uppercase",
                      fontSize: "0.68rem",
                      fontWeight: 700,
                      letterSpacing: "0.1em",
                      borderBottom: "none",
                      px: 2,
                      py: 0.5,
                    }}
                  >
                    Nome
                  </TableCell>
                  <TableCell
                    sx={{
                      background: "background.default",
                      color: c.tableHeaderText,
                      textTransform: "uppercase",
                      fontSize: "0.68rem",
                      fontWeight: 700,
                      letterSpacing: "0.1em",
                      borderBottom: "none",
                      px: 2,
                      py: 0.5,
                    }}
                  >
                    Descrizione
                  </TableCell>
                  <TableCell
                    sx={{
                      background: "background.default",
                      color: c.tableHeaderText,
                      textTransform: "uppercase",
                      fontSize: "0.68rem",
                      fontWeight: 700,
                      letterSpacing: "0.1em",
                      borderBottom: "none",
                      px: 2,
                      py: 0.5,
                    }}
                  >
                    Tag
                  </TableCell>
                  <TableCell
                    align="right"
                    sx={{
                      background: "background.default",
                      color: c.tableHeaderText,
                      textTransform: "uppercase",
                      fontSize: "0.68rem",
                      fontWeight: 700,
                      letterSpacing: "0.1em",
                      borderBottom: "none",
                      px: 2,
                      py: 0.5,
                      width: 120,
                    }}
                  >
                    Priorità
                  </TableCell>
                  <TableCell
                    sx={{
                      background: "background.default",
                      color: c.tableHeaderText,
                      textTransform: "uppercase",
                      fontSize: "0.68rem",
                      fontWeight: 700,
                      letterSpacing: "0.1em",
                      borderBottom: "none",
                      px: 2,
                      py: 0.5,
                      width: 120,
                    }}
                  >
                    Azioni
                  </TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {categories.map((category) => (
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
                      sx={{
                        borderBottom: "none",
                        backgroundColor: c.rowBackground,
                        py: 1.5,
                      }}
                    >
                      <IconButton
                        aria-label="Azioni categoria"
                        aria-controls={
                          isActionsMenuOpen ? `category-actions-${category.id}` : undefined
                        }
                        aria-expanded={isActionsMenuOpen ? "true" : undefined}
                        aria-haspopup="true"
                        onClick={(event) => openActionsMenu(event, category)}
                        sx={{
                          color: c.actionButtonColor,
                          border: `1px solid ${c.actionButtonBorder}`,
                          backgroundColor: c.actionButtonBackground,
                          "&:hover": {
                            color: theme.palette.text.secondary,
                            backgroundColor: c.actionButtonHover,
                          },
                        }}
                      >
                        <MoreVertRounded />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </TableContainer>
      </Paper>

      <Menu
        id={selectedCategory ? `category-actions-${selectedCategory.id}` : "category-actions"}
        anchorEl={menuAnchorEl}
        open={isActionsMenuOpen}
        onClose={closeActionsMenu}
      >
        <MenuItem onClick={handleEditRequest}>
          <ListItemIcon>
            <EditRounded fontSize="small" />
          </ListItemIcon>
          <ListItemText>Modifica</ListItemText>
        </MenuItem>
        <MenuItem
          onClick={handleDeleteRequest}
          disabled={selectedCategory?.isDefault === true}
        >
          <ListItemIcon>
            <DeleteOutlineRounded fontSize="small" />
          </ListItemIcon>
          <ListItemText>Elimina</ListItemText>
        </MenuItem>
      </Menu>

      <Dialog
        open={deleteDialogOpen}
        onClose={handleDeleteCancel}
        aria-labelledby="delete-category-title"
        aria-describedby="delete-category-description"
      >
        <DialogTitle id="delete-category-title">Conferma eliminazione</DialogTitle>
        <DialogContent>
          <DialogContentText id="delete-category-description">
            Vuoi eliminare la categoria "{selectedCategory?.name}"? Le transazioni
            collegate verranno riassegnate alla categoria di default.
          </DialogContentText>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={handleDeleteCancel} color="inherit">
            Annulla
          </Button>
          <Button onClick={() => void handleDeleteConfirm()} color="error" variant="contained">
            Elimina
          </Button>
        </DialogActions>
      </Dialog>

      <Modal
        open={modalOpen}
        onClose={closeModal}
        aria-labelledby="category-modal-title"
        slotProps={{
          backdrop: {
            sx: {
              backgroundColor:
                theme.palette.mode === "dark"
                  ? "rgba(2, 6, 23, 0.7)"
                  : "rgba(15, 23, 42, 0.35)",
              backdropFilter: "blur(2px)",
            },
          },
        }}
      >
        <Box
          sx={{
            position: "absolute",
            top: "50%",
            left: "50%",
            transform: "translate(-50%, -50%)",
            width: { xs: "calc(100% - 24px)", sm: 460 },
            maxWidth: 460,
            bgcolor: c.drawerBackground,
            border: `1px solid ${c.drawerBorder}`,
            borderRadius: "14px",
            boxShadow:
              theme.palette.mode === "dark"
                ? "0 16px 40px rgba(0,0,0,0.45)"
                : "0 16px 40px rgba(15,23,42,0.16)",
            p: { xs: 2, sm: 3 },
          }}
        >
          <form onSubmit={(event) => void handleSubmit(event)}>
            <Stack direction="column" spacing={2}>
              <Typography id="category-modal-title" variant="h6" component="h2" sx={{ fontWeight: 600 }}>
                {editingCategory ? "Modifica categoria" : "Nuova categoria"}
              </Typography>

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
                  setForm((prev) => ({ ...prev, description: event.target.value }))
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

              <Stack direction="row" spacing={1.25} justifyContent="flex-end" sx={{ pt: 1 }}>
                <Button
                  variant="outlined"
                  onClick={closeModal}
                  disabled={operationInProgress}
                  sx={{
                    minWidth: 120,
                    borderColor: c.filterBorder,
                    color: theme.palette.text.secondary,
                    "&:hover": {
                      borderColor: c.drawerBorder,
                      backgroundColor: c.filterBackground,
                    },
                  }}
                >
                  Annulla
                </Button>
                <Button
                  variant="contained"
                  type="submit"
                  disabled={operationInProgress}
                  sx={{
                    minWidth: 120,
                    backgroundColor: c.accentColor,
                    color: theme.palette.mode === "dark" ? "#0f172a" : "#ffffff",
                    "&:hover": {
                      backgroundColor:
                        theme.palette.mode === "dark" ? "#bef264" : "#4d7c0f",
                    },
                  }}
                >
                  {operationInProgress ? "Salvataggio..." : "Salva"}
                </Button>
              </Stack>
            </Stack>
          </form>
        </Box>
      </Modal>

      <Snackbar
        open={successOpen}
        autoHideDuration={4000}
        onClose={() => setSuccessOpen(false)}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      >
        <Alert
          severity="success"
          variant="filled"
          onClose={() => setSuccessOpen(false)}
          sx={{ width: "100%", minWidth: 320 }}
        >
          {successMessage ?? "Operazione completata"}
        </Alert>
      </Snackbar>
    </main>
  );
}
