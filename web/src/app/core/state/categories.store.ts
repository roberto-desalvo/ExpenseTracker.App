import { Injectable, inject } from "@angular/core";
import { CategoryApi, CategorySyncRequest, CategorySyncResult } from "../api/category.api";
import Category from "../models/Category";
import { PagedEntityStore, PagedQuery } from "./paged-entity.store";

/** Categorie: `all` = elenco completo, `paged` = lista della vista Impostazioni → Categorie. */
@Injectable({ providedIn: "root" })
export class CategoriesStore extends PagedEntityStore<Category> {
  private readonly api = inject(CategoryApi);

  protected query(request: PagedQuery) {
    return this.api.getAll(request);
  }

  async addCategory(category: Category) {
    await this.api.add(category);
    await this.reloadFromFirstPage();
  }

  async updateCategory(category: Category) {
    await this.api.update(category);
    await this.reloadBoth();
  }

  async deleteCategory(id: number) {
    await this.api.delete(id);
    await this.reloadBoth();
  }

  /** Riassegna la categoria ai movimenti del periodo in base alle parole chiave (trasferimenti esclusi). */
  syncTransactions(request: CategorySyncRequest): Promise<CategorySyncResult> {
    return this.api.syncTransactions(request);
  }
}
