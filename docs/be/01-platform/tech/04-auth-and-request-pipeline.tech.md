# 04 - Auth and request pipeline (Tecnico)

## Autenticazione & autorizzazione
Azure AD (Entra ID) JWT bearer via `Microsoft.Identity.Web` (`AddMicrosoftIdentityWebApi`, sezione config `AzureAd`). Tutte le route richiedono autorizzazione di default; `ImportController` richiede in aggiunta il ruolo `Files.Sender`.

## CORS
Due policy nominate, selezionate in base all'ambiente (`Program.cs`):
- **`FEPolicy`** (non-development) — origine da `App:FEUrl`.
- **`DebugPolicy`** (solo development) — origini da `App:AllowedLocalOrigins`.

## Middleware (`Api/Middlewares/`)
- **`ExceptionHandlingMiddleware`** — cattura `DomainException` → `ProblemDetails` con lo status/title dell'eccezione; qualunque altra eccezione → 500. Vedi [05-error-handling](05-error-handling.tech.md).
- **`RequestLoggingMiddleware`** — logga metodo, path, IP remoto per ogni richiesta.

## Ordine pipeline (`Program.cs`)
`UseHttpsRedirection` → `UseCors` → `UseAuthentication` → `UseAuthorization` → `ExceptionHandlingMiddleware` → `UseStatusCodePages` → `RequestLoggingMiddleware` → `MapOpenApi`/`MapScalarApiReference` → `MapControllers().RequireAuthorization()`.

`ExceptionHandlingMiddleware` è registrato **dopo** `UseAuthentication`/`UseAuthorization`: copre solo le eccezioni da routing/controller, non quelle sollevate dall'auth stessa.

## OpenAPI / docs
`AddOpenApi()`/`MapOpenApi()` (built-in .NET, non Swashbuckle) + **Scalar** (`Scalar.AspNetCore`) come UI interattiva su `/scalar`.

## Opzioni di configurazione (`Api/Options/`)
Ogni classe implementa `IAppOptions` e si lega a una sezione di config; `OptionsRegistrationExtensions.AddOptions()` le registra per riflessione. Il Domain dipende dalle interfacce `I*Options`, con un adapter `IOptions<T>.Value` registrato in `Program.cs`.

| Classe | Sezione | Scopo |
|---|---|---|
| `AzureAdOptions` | `AzureAd` | Registrazione app Entra ID |
| `AppOptions` | `App` | `FEUrl`, `AllowedLocalOrigins` (CORS) |
| `ExpenseExcelFileOptions` | `ExpenseTrackerExcel` | Indici colonna importer Excel legacy |
| `BbvaCsvOptions` / `SellaCsvOptions` / `SatisPayCsvOptions` / `TradeRepublicCsvOptions` | `BbvaCsv` / `SellaCsv` / `SatisPayCsv` / `TradeRepublicCsv` | Default e IBAN-to-account map per importer |
| `TransferMatchingOptions` | `TransferMatching` | `Rules[]` per il transfer matching |
| `KeyVaultOptions` | `KeyVault` | Non usata a runtime (wiring commentato in `Program.cs`) |
