import { Injectable } from "@angular/core";
import { MatPaginatorIntl } from "@angular/material/paginator";

@Injectable()
export class ItalianPaginatorIntl extends MatPaginatorIntl {
  override itemsPerPageLabel = "Per pagina";
  override nextPageLabel = "Pagina successiva";
  override previousPageLabel = "Pagina precedente";
  override firstPageLabel = "Prima pagina";
  override lastPageLabel = "Ultima pagina";
  override getRangeLabel = (page: number, pageSize: number, length: number) => {
    if (length === 0) return `0 di 0`;
    const from = page * pageSize + 1;
    const to = Math.min((page + 1) * pageSize, length);
    return `${from}–${to} di ${length}`;
  };
}
