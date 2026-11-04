# 02 - Bootstrap and providers (Tecnico)

## Flusso di avvio
1. `src/main.ts` chiama `bootstrapApplication(App, appConfig)`.
2. `provideAppInitializer` (in `app.config.ts`) esegue `AuthService.initialize()` **prima del primo render**: con MSAL attivo inizializza il singleton (`msalInstance.initialize()`), gestisce il ritorno dal redirect di Entra ID (`handleRedirectPromise`, imposta l'account attivo) e ricostruisce la sessione (demo, MSAL o utente di sviluppo). Con il bypass di sviluppo legge solo il nome utente salvato (modulo 04).
3. `App` (`src/app/app.ts`) istanzia subito `ThemeService` (i token dello stile valgono anche per la pagina di login) e imposta `material-symbols-rounded` come font set di default di `mat-icon`. Poi mostra `AppLayout` se `AuthService.isAuthenticated()`, altrimenti `DevLoginPage` (bypass) o `LoginPage`. L'URL richiesto non cambia: dopo il login si atterra sulla pagina di partenza.
4. `AppLayout` (shell) chiede una sola volta l'anagrafica di conti e categorie (`AccountsStore.ensureLoaded()`, `CategoriesStore.ensureLoaded()`) e monta una sola istanza di `TransactionModal` e `TransferModal`.

## Provider globali (`src/app/app.config.ts`)
- `provideBrowserGlobalErrorListeners()`
- `provideRouter(routes, withInMemoryScrolling({ scrollPositionRestoration: "top" }))` — a ogni navigazione la pagina torna in cima.
- `provideHttpClient(withInterceptors([authInterceptor, errorInterceptor]))` — modulo 05.
- `provideAppInitializer(() => inject(AuthService).initialize())`.
- `provideDateFnsAdapter(IT_DATE_FORMATS)` + `MAT_DATE_LOCALE = it` (date-fns) e `LOCALE_ID = "it-IT"` (con `registerLocaleData(localeIt)`).
- `MatPaginatorIntl` → `ItalianPaginatorIntl` (`src/app/shared/ui/paginator-intl.ts`).
- `MAT_FORM_FIELD_DEFAULT_OPTIONS` → `{ appearance: "outline", subscriptSizing: "dynamic" }`.
- `provideEchartsCore({ echarts: () => import("./shared/charts/echarts-setup") ... })` — ECharts viene scaricato solo alla prima pagina con un grafico.

## Stato applicativo
Non ci sono provider annidati: tutti gli store sono servizi `@Injectable({ providedIn: "root" })` istanziati alla prima iniezione (modulo 07).

## Motivazione
Autenticazione e tema sono risolti prima di mostrare qualsiasi contenuto; l'anagrafica condivisa è pronta non appena l'utente entra nell'area protetta.

## File sorgente
- `src/main.ts`, `src/app/app.config.ts`, `src/app/app.ts`, `src/app/app.routes.ts`
- `src/app/core/auth/auth.service.ts`, `msalInstance.ts`, `authConfig.ts`, `devAuth.ts`, `demoAuth.ts`
- `src/app/core/theme/theme.service.ts`
- `src/app/layout/app-layout.ts`
