import { COMMA, ENTER } from "@angular/cdk/keycodes";
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from "@angular/core";
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from "@angular/material/autocomplete";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatChipInputEvent, MatChipsModule } from "@angular/material/chips";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatTooltip } from "@angular/material/tooltip";
import { NotificationService } from "../../core/api/notification.service";
import Category from "../../core/models/Category";
import { CategoriesStore } from "../../core/state/categories.store";
import { initialsOf } from "../../core/utils/format.utilities";
import { ConfirmDeleteDialog } from "../../shared/ui/confirm-delete-dialog";
import { DataTable, DataTableColumn } from "../../shared/ui/data-table";
import { EditorDrawer } from "../../shared/ui/editor-drawer";
import { SearchField } from "../../shared/ui/search-field";
import { CategorySyncModal } from "./category-sync-modal";

interface CategoryFormState {
  name: string;
  description: string;
  priority: string;
  tags: string[];
}

const createInitialForm = (): CategoryFormState => ({ name: "", description: "", priority: "0", tags: [] });

const COLUMNS: DataTableColumn[] = [
  { id: "name", label: "Categoria", minWidth: "36%" },
  { id: "tags", label: "Parole chiave", minWidth: "40%" },
  { id: "priority", label: "Priorità", minWidth: "10%", align: "right" },
  { id: "actions", label: "", minWidth: "14%", align: "right" },
];

/** Impostazioni → Categorie: elenco paginato, parole chiave per la classificazione automatica, sincronizzazione. */
@Component({
  selector: "app-categories-settings",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButton,
    MatIconButton,
    MatIconModule,
    MatTooltip,
    MatFormFieldModule,
    MatInputModule,
    MatChipsModule,
    MatAutocompleteModule,
    DataTable,
    EditorDrawer,
    SearchField,
    ConfirmDeleteDialog,
    CategorySyncModal,
  ],
  templateUrl: "./categories-settings.html",
  styleUrl: "./categories-settings.scss",
})
export class CategoriesSettings {
  protected readonly store = inject(CategoriesStore);
  private readonly notifications = inject(NotificationService);

  protected readonly columns = COLUMNS;
  protected readonly separatorKeys = [ENTER, COMMA] as const;
  protected readonly initialsOf = initialsOf;

  protected readonly form = signal<CategoryFormState>(createInitialForm());
  protected readonly editorOpen = signal(false);
  protected readonly editing = signal<Category | null>(null);
  protected readonly toDelete = signal<Category | null>(null);
  protected readonly busy = signal(false);
  protected readonly syncOpen = signal(false);
  protected readonly tagInput = signal("");

  private readonly knownTags = computed(() =>
    [...new Set(this.store.all().flatMap((c) => c.tags ?? []))].sort((x, y) => x.localeCompare(y)),
  );
  protected readonly tagSuggestions = computed(() => {
    const selected = new Set(this.form().tags);
    const query = this.tagInput().trim().toLowerCase();
    return this.knownTags().filter((t) => !selected.has(t) && (!query || t.toLowerCase().includes(query)));
  });

  protected readonly deleteMessage = computed(
    () => `I movimenti collegati a “${this.toDelete()?.name ?? ""}” verranno riassegnati alla categoria predefinita.`,
  );

  constructor() {
    // la lista paginata si carica solo mentre questa sezione è visibile
    inject(DestroyRef).onDestroy(this.store.activatePagedList());
  }

  protected inputValue(event: Event) {
    return (event.target as HTMLInputElement).value;
  }

  protected patchForm(changes: Partial<CategoryFormState>) {
    this.form.update((f) => ({ ...f, ...changes }));
  }

  protected openCreate() {
    this.editing.set(null);
    this.form.set(createInitialForm());
    this.editorOpen.set(true);
  }

  protected openEdit(category: Category) {
    this.editing.set(category);
    this.form.set({
      name: category.name,
      description: category.description ?? "",
      priority: String(category.priority ?? 0),
      tags: category.tags ?? [],
    });
    this.editorOpen.set(true);
  }

  protected addTag(value: string) {
    const tag = value.trim();
    if (tag && !this.form().tags.includes(tag)) {
      this.patchForm({ tags: [...this.form().tags, tag] });
    }
    this.tagInput.set("");
  }

  protected onTagInputEnd(event: MatChipInputEvent) {
    this.addTag(event.value);
    event.chipInput.clear();
  }

  protected onTagSelected(event: MatAutocompleteSelectedEvent, input: HTMLInputElement) {
    this.addTag(event.option.viewValue);
    input.value = "";
    event.option.deselect();
  }

  protected removeTag(tag: string) {
    this.patchForm({ tags: this.form().tags.filter((t) => t !== tag) });
  }

  protected async submit() {
    const form = this.form();
    const editing = this.editing();
    const payload: Category = {
      id: editing?.id ?? 0,
      name: form.name.trim(),
      description: form.description.trim(),
      priority: Number(form.priority) || 0,
      isDefault: editing?.isDefault ?? false,
      tags: form.tags.map((t) => t.trim()).filter(Boolean),
    };

    if (!payload.name) return;

    this.busy.set(true);
    try {
      if (payload.id > 0) {
        await this.store.updateCategory(payload);
        this.notifications.showSuccess(`Categoria "${payload.name}" modificata`);
      } else {
        await this.store.addCategory(payload);
        this.notifications.showSuccess(`Categoria "${payload.name}" creata`);
      }
      this.editorOpen.set(false);
    } catch {
      // errore mostrato dal layer API
    } finally {
      this.busy.set(false);
    }
  }

  protected readonly confirmDelete = async (): Promise<string | void> => {
    const category = this.toDelete();
    if (!category) return;
    this.busy.set(true);
    try {
      await this.store.deleteCategory(category.id);
      this.toDelete.set(null);
      this.editorOpen.set(false);
      return `Categoria "${category.name}" eliminata`;
    } finally {
      this.busy.set(false);
    }
  };
}
