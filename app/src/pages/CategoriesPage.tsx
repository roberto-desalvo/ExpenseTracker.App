import { useEffect, useMemo, useState } from "react";
import {
  Alert,
  Box,
  Checkbox,
  Chip,
  CircularProgress,
  FormControl,
  InputLabel,
  ListItemText,
  MenuItem,
  OutlinedInput,
  Select,
  Stack,
  Tab,
  TableCell,
  TableRow,
  Tabs,
  TextField,
  Typography,
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
import TransactionService from "../services/TransactionService";
import { TimeSeriesList } from "../models/TimeSeries";
import TimeSeriesLineChart, {
  TimeSeriesLineChartSeries,
} from "../components/TimeSeriesLineChart";
import { toIsoDateStart, toIsoDateEnd } from "../utilities/date.utilities";

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
  const [modalOpen, setModalOpen] = useState<boolean>(false);
  const [editingCategory, setEditingCategory] = useState<Category | null>(null);
  const [selectedCategory, setSelectedCategory] = useState<Category | null>(
    null,
  );
  const [deleteDialogOpen, setDeleteDialogOpen] = useState<boolean>(false);
  const [operationInProgress, setOperationInProgress] =
    useState<boolean>(false);
  const [activeTab, setActiveTab] = useState<number>(0);

  const [dashboardLoading, setDashboardLoading] = useState<boolean>(false);
  const [dashboardData, setDashboardData] = useState<TimeSeriesList | null>(
    null,
  );
  const [dashboardStartDate, setDashboardStartDate] = useState<string>(() => {
    const now = new Date();
    const start = new Date(now.getFullYear() - 1, now.getMonth(), now.getDate());
    return start.toISOString().slice(0, 10);
  });
  const [dashboardEndDate, setDashboardEndDate] = useState<string>(() => {
    const now = new Date();
    return now.toISOString().slice(0, 10);
  });
  const [dashboardGranularity, setDashboardGranularity] = useState<number>(3);
  const [dashboardCategoryIds, setDashboardCategoryIds] = useState<number[]>([]);

  const columns: DataTableColumn[] = [
    { id: "name", label: "Nome", minWidth: 150 },
    { id: "description", label: "Descrizione", minWidth: 200 },
    { id: "tags", label: "Tag", minWidth: 150 },
    { id: "priority", label: "Priorità", minWidth: 80, align: "right" },
    { id: "actions", label: "Azioni", minWidth: 80, align: "center" },
  ];

  useEffect(() => {
    if (allCategories.length === 0) {
      setDashboardCategoryIds([]);
      return;
    }

    setDashboardCategoryIds((prev) => {
      if (prev.length === 0) {
        return allCategories.map((category) => category.id);
      }

      return prev.filter((id) => allCategories.some((category) => category.id === id));
    });
  }, [allCategories]);

  const categoryNameById = useMemo(
    () => new Map(allCategories.map((category) => [category.id, category.name])),
    [allCategories],
  );

  const chartSeries = useMemo<TimeSeriesLineChartSeries[]>(() => {
    if (!dashboardData) {
      return [];
    }

    return dashboardData.series.map((serie, index) => {
      const categoryDimension = serie.dimensions.find(
        (dimension) => dimension.key === "CategoryId",
      );

      const categoryId = categoryDimension ? Number(categoryDimension.value) : NaN;
      const categoryName = Number.isFinite(categoryId)
        ? (categoryNameById.get(categoryId) ?? `Categoria ${categoryId}`)
        : `Serie ${index + 1}`;

      return {
        name: categoryName,
        values: serie.values,
      };
    });
  }, [dashboardData, categoryNameById]);

  const handleDashboardLoad = async () => {
    if (!dashboardStartDate || !dashboardEndDate) {
      return;
    }

    setDashboardLoading(true);
    try {
      const result = await TransactionService.getTimeSeries({
        startDate: toIsoDateStart(dashboardStartDate),
        endDate: toIsoDateEnd(dashboardEndDate),
        idAccounts: [],
        idCategories: dashboardCategoryIds.length > 0 ? dashboardCategoryIds : [],
        granularity: dashboardGranularity,
      });
      setDashboardData(result);
    } finally {
      setDashboardLoading(false);
    }
  };

  useEffect(() => {
    if (activeTab !== 1) {
      return;
    }

    void handleDashboardLoad();
  }, [activeTab]);

  useEffect(() => {
    if (activeTab !== 1 || !dashboardStartDate || !dashboardEndDate) {
      return;
    }

    void handleDashboardLoad();
  }, [dashboardStartDate, dashboardEndDate, dashboardGranularity, dashboardCategoryIds, activeTab]);

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
          Categorie
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
        {activeTab === 0 && (
          <CategoriesFilterBar
            onSearch={handleSearch}
            onAddClick={openCreateModal}
            onRefresh={() => void refreshCategories()}
            isLoading={isLoading}
          />
        )}
        <main className="px-2 pb-2">
          {activeTab === 0 ? (
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
          ) : (
            <Box sx={{ p: 2 }}>
            <Stack spacing={2.5}>
              <Stack direction={{ xs: "column", md: "row" }} spacing={1.5}>
                <TextField
                  label="Data inizio"
                  type="date"
                  value={dashboardStartDate}
                  onChange={(event) => setDashboardStartDate(event.target.value)}
                  InputLabelProps={{ shrink: true }}
                  size="small"
                />
                <TextField
                  label="Data fine"
                  type="date"
                  value={dashboardEndDate}
                  onChange={(event) => setDashboardEndDate(event.target.value)}
                  InputLabelProps={{ shrink: true }}
                  size="small"
                />

                <FormControl size="small" sx={{ minWidth: 180 }}>
                  <InputLabel id="granularity-label">Granularità</InputLabel>
                  <Select
                    labelId="granularity-label"
                    value={dashboardGranularity}
                    label="Granularità"
                    onChange={(event) =>
                      setDashboardGranularity(Number(event.target.value))
                    }
                  >
                    <MenuItem value={1}>Giornaliero</MenuItem>
                    <MenuItem value={2}>Settimanale</MenuItem>
                    <MenuItem value={3}>Mensile</MenuItem>
                    <MenuItem value={4}>Annuale</MenuItem>
                  </Select>
                </FormControl>
              </Stack>

              <Stack direction={{ xs: "column", md: "row" }} spacing={1.5}>
                <FormControl size="small" sx={{ minWidth: 280, flex: 1.3 }}>
                  <InputLabel id="dashboard-category-filter-label">Categorie</InputLabel>
                  <Select
                    labelId="dashboard-category-filter-label"
                    multiple
                    value={dashboardCategoryIds.map(String)}
                    onChange={(event) => {
                      const value = event.target.value;
                      const ids =
                        typeof value === "string"
                          ? value.split(",").map((id) => Number(id))
                          : value.map((id) => Number(id));
                      setDashboardCategoryIds(ids);
                    }}
                    input={<OutlinedInput label="Categorie" />}
                    renderValue={(selected) => {
                      if (selected.length === 0) return "Nessuna categoria";
                      return selected
                        .map((id) =>
                          allCategories.find((category) => category.id === Number(id))?.name,
                        )
                        .filter(Boolean)
                        .join(", ");
                    }}
                  >
                    {allCategories.map((category) => (
                      <MenuItem key={category.id} value={String(category.id)}>
                        <Checkbox checked={dashboardCategoryIds.includes(category.id)} />
                        <ListItemText primary={category.name} />
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>

              </Stack>

              {dashboardStartDate > dashboardEndDate && (
                <Alert severity="warning">
                  La data di inizio deve essere precedente o uguale alla data di fine.
                </Alert>
              )}

              <Box
                sx={{
                  border: `1px solid ${c.filterBorder}`,
                  borderRadius: 2,
                  backgroundColor: theme.palette.background.paper,
                  p: 2,
                  minHeight: 430,
                }}
              >
                {dashboardLoading ? (
                  <Box
                    sx={{
                      height: 390,
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "center",
                    }}
                  >
                    <CircularProgress size={28} />
                  </Box>
                ) : (
                  <TimeSeriesLineChart
                    series={chartSeries}
                    emptyMessage="Nessuna serie disponibile per i filtri selezionati"
                  />
                )}
              </Box>
            </Stack>
            </Box>
          )}

          <ConfirmDeleteDialog
            open={deleteDialogOpen}
            onClose={handleDeleteCancel}
            onConfirm={handleDeleteConfirm}
            isBusy={operationInProgress}
            message={
              <>
                Vuoi eliminare la categoria &ldquo;{selectedCategory?.name}&rdquo;?
                Le transazioni collegate verranno riassegnate alla categoria di
                default.
              </>
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
      </Stack>
    </>
  );
}
