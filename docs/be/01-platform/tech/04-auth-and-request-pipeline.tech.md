# 04 - Auth and request pipeline (Tecnico)

## Autenticazione & autorizzazione
Azure AD (Entra ID) JWT bearer via `Microsoft.Identity.Web` (`AddMicrosoftIdentityWebApi`, sezione config `AzureAd`). Tutte le route richiedono autorizzazione di default; `ImportController` richiede in aggiunta il ruolo `Files.Sender`.

## Utente corrente & JIT provisioning (`Api/Auth/CurrentUserAccessor.cs`)
`ICurrentUserAccessor` (`Domain/Services/`, singolo metodo `Task<int> GetUserIdAsync()`) risolve l'utente interno (`User.Id`) a partire dal `ClaimsPrincipal` della richiesta corrente:
1. Estrae l'oid Azure AD tramite `principal.GetObjectId()` (extension method di `Microsoft.Identity.Web`) — **non** `ClaimTypes.NameIdentifier`, usato erroneamente in una versione precedente di `AuthController`.
2. Estrae l'email da `ClaimTypes.Email`, con fallback al claim `preferred_username`.
3. Chiama `IUserService.GetOrCreateUserAsync(oid, email, name)`, che fa da layer applicativo (validazione + mapping a `UserDto`) sopra `IUserRepository.GetOrCreateUserAsync(azureOid, email)`: cerca l'utente per `AzureOid`, lo crea al volo se non esiste (**JIT — Just-In-Time provisioning**), e gestisce le race condition concorrenti ritentando la lettura in caso di violazione dell'indice univoco filtrato su `AzureOid` (`SqlException` 2601/2627).
4. Se manca l'oid o l'email nel token, o il provisioning fallisce, viene sollevata `UnauthorizedDomainException`.

Il risultato è cachato in un campo privato per la durata della request (`CurrentUserAccessor` è registrato **Scoped** in `Program.cs`), quindi chiamate multiple a `GetUserIdAsync()` nello stesso request scope non ripetono la query.

Usato da `AuthController` (`GET /api/Auth` → `{ UserId }`, non più email/name grezzi dal token), `AccountController` e `ImportController` (risolto a inizio di ogni action, per tutti e 16 gli endpoint di import) per scopare le query per utente.

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
