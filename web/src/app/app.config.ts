import { provideHttpClient, withInterceptors } from "@angular/common/http";
import {
  ApplicationConfig,
  LOCALE_ID,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from "@angular/core";
import { provideDateFnsAdapter } from "@angular/material-date-fns-adapter";
import { MAT_DATE_LOCALE } from "@angular/material/core";
import { MAT_FORM_FIELD_DEFAULT_OPTIONS } from "@angular/material/form-field";
import { MatPaginatorIntl } from "@angular/material/paginator";
import { provideRouter, withInMemoryScrolling } from "@angular/router";
import { registerLocaleData } from "@angular/common";
import localeIt from "@angular/common/locales/it";
import { it } from "date-fns/locale";
import { provideEchartsCore } from "ngx-echarts";
import { routes } from "./app.routes";
import { authInterceptor, errorInterceptor } from "./core/api/interceptors";
import { AuthService } from "./core/auth/auth.service";
import { ItalianPaginatorIntl } from "./shared/ui/paginator-intl";

registerLocaleData(localeIt);

const IT_DATE_FORMATS = {
  parse: { dateInput: ["dd/MM/yyyy", "d/M/yyyy", "dd/MM/yy"] },
  display: {
    dateInput: "dd/MM/yyyy",
    monthYearLabel: "MMM yyyy",
    dateA11yLabel: "d MMMM yyyy",
    monthYearA11yLabel: "MMMM yyyy",
  },
};

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withInMemoryScrolling({ scrollPositionRestoration: "top" })),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    // MSAL va inizializzato (e il ritorno dal redirect gestito) prima del primo render
    provideAppInitializer(() => inject(AuthService).initialize()),
    provideDateFnsAdapter(IT_DATE_FORMATS),
    { provide: MAT_DATE_LOCALE, useValue: it },
    { provide: LOCALE_ID, useValue: "it-IT" },
    { provide: MatPaginatorIntl, useClass: ItalianPaginatorIntl },
    // campi sempre "outlined"; lo spazio per hint/errori compare solo quando serve
    { provide: MAT_FORM_FIELD_DEFAULT_OPTIONS, useValue: { appearance: "outline", subscriptSizing: "dynamic" } },
    // ECharts viene caricato solo quando una pagina mostra un grafico
    provideEchartsCore({ echarts: () => import("./shared/charts/echarts-setup").then((m) => m.echarts) }),
  ],
};
