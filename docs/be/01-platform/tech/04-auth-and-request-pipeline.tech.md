# 04 - Auth and request pipeline (Tecnico)

## Autenticazione & autorizzazione
Azure AD (Entra ID) JWT bearer, validato dal worker stesso (le function HTTP sono `AuthorizationLevel.Anonymous`, quindi niente function key né Easy Auth):

- **`Functions/Auth/JwtTokenValidator.cs`** (singleton) — scarica e cacha i metadata OIDC da `{AzureAd:Instance}{AzureAd:TenantId}/v2.0/.well-known/openid-configuration` (`ConfigurationManager<OpenIdConnectConfiguration>`) e valida il token con `JsonWebTokenHandler` (`MapInboundClaims = true`, quindi gli stessi claim type mappati della precedente pipeline `Microsoft.Identity.Web`). Controlli: firma, lifetime, issuer (v1 `https://sts.windows.net/{tid}/` e v2 `https://login.microsoftonline.com/{tid}/v2.0`), audience (`AzureAd:Audience`, `AzureAd:ClientId`, `api://{ClientId}`). Su chiave di firma sconosciuta forza il refresh dei metadata.
- **`Functions/Middlewares/AuthenticationMiddleware.cs`** — per ogni invocazione HTTP legge `Authorization: Bearer ...`, valida il token e salva il `ClaimsPrincipal` sia in `HttpContext.User` sia nel servizio scoped `RequestPrincipalAccessor`. Token assente/non valido → `UnauthorizedDomainException` (401).
- **Attributi (`Functions/Auth/AuthorizationAttributes.cs`)** — `[RequireRole("...")]` (su classe o metodo) richiede almeno uno dei ruoli app (claim `roles`), altrimenti `ForbiddenDomainException` (403); `[AllowAnonymousFunction]` salta la validazione. Il middleware risolve gli attributi per riflessione da `FunctionDefinition.EntryPoint` e li cacha per function. `ImportFunctions` è decorata con `[RequireRole("Files.Sender")]`; nessuna function è attualmente anonima.

## Utente corrente & JIT provisioning (`Functions/Auth/CurrentUserAccessor.cs`)
`ICurrentUserAccessor` (`Domain/Services/`) risolve l'utente interno (`User.Id`) a partire dal `ClaimsPrincipal` dell'invocazione corrente (letto da `RequestPrincipalAccessor`), con due metodi per due scenari diversi:

### `GetUserIdAsync()` — utenti umani (`AuthFunctions`, `AccountFunctions`, `DemoFunctions`)
1. Estrae l'oid Azure AD dal claim `http://schemas.microsoft.com/identity/claims/objectidentifier` (fallback `oid`) — **non** `ClaimTypes.NameIdentifier`.
2. Estrae l'email da `ClaimTypes.Email`, con fallback ai claim `email` e `preferred_username`; se assente, **solleva subito** `UnauthorizedDomainException`.
3. Chiama `IUserService.GetOrCreateUserAsync(oid, email, name)`, che fa da layer applicativo (validazione + mapping a `UserDto`) sopra `IUserRepository.GetOrCreateUserAsync(azureOid, email)`: cerca l'utente per `AzureOid`, lo crea al volo se non esiste (**JIT — Just-In-Time provisioning**), e gestisce le race condition concorrenti ritentando la lettura in caso di violazione dell'indice univoco filtrato su `AzureOid` (`SqlException` 2601/2627).
4. Se manca l'oid nel token, o il provisioning fallisce, viene sollevata `UnauthorizedDomainException`.

### `GetUserIdForImportAsync()` — solo `ImportFunctions` (chiamato anche da app/managed identity)
Il flusso di import è chiamato anche da un'app Azure con **managed identity**, il cui token (app-only, client credentials) ha un `oid` diverso da qualunque utente umano e tipicamente **non porta claim `email`/`name`**. Questo metodo:
1. Estrae l'oid come sopra (sempre richiesto).
2. Estrae l'email **senza sollevare eccezione se assente** (`email` può essere `null`).
3. Chiama `IUserService.GetOrCreateUserForImportAsync(oid, email, name)`, che prova in ordine: `IUserRepository.GetByAzureOid(oid)` → `IUserRepository.GetByAppOid(oid)` (fallback: l'oid della managed identity è stato pre-associato manualmente via SQL a un utente esistente, colonna `User.AppOid`) → se nessuno dei due trova un utente e `email` è presente, crea un nuovo utente (stesso path di `GetOrCreateUserAsync`) → altrimenti fallisce con `DomainErrors.Unauthorized` (nessuna creazione automatica di utenti senza email).
4. Se il provisioning/lookup fallisce, viene sollevata `UnauthorizedDomainException` (401) — stessa gestione di `GetUserIdAsync()`.

Entrambi i metodi condividono lo stesso `_cachedUserId` per invocazione (nessuna function chiama entrambi i metodi nello stesso scope).

Il risultato è cachato in un campo privato per la durata dell'invocazione (`CurrentUserAccessor` e `RequestPrincipalAccessor` sono registrati **Scoped** in `Program.cs`; nel worker isolato lo scope DI coincide con la singola invocazione di function).

## CORS
Gestito dalla **piattaforma Azure Functions**, non dal codice:
- **Azure** — origini consentite nella configurazione CORS della Function App (portale o `az functionapp cors add`). Il workflow di deploy aggiunge `vars.FE_URL` se sono valorizzate le variabili `AZURE_RESOURCE_GROUP` e `FE_URL` (vedi [12-cicd-and-configuration](../../04-operations/tech/12-cicd-and-configuration.tech.md)).
- **Locale** — `Host.CORS` in `local.settings.json` (default `http://localhost:5173`, dev server Vite).

`AppOptions` (`App:FEUrl`, `App:AllowedLocalOrigins`) resta bindata ma non è più usata a runtime.

## Middleware worker (`Functions/Middlewares/`)
Implementano `IFunctionsWorkerMiddleware`, registrati in `Program.cs` in quest'ordine:
1. **`RequestLoggingMiddleware`** — logga metodo, path, IP remoto e nome della function.
2. **`ExceptionHandlingMiddleware`** — cattura `DomainException` (anche se wrappata come `InnerException`) → `ProblemDetails` con lo status/title dell'eccezione; qualunque altra eccezione → 500. Vedi [05-error-handling](05-error-handling.tech.md).
3. **`AuthenticationMiddleware`** — vedi sopra.

Diversamente dalla precedente Web API, `ExceptionHandlingMiddleware` è **esterno** all'autenticazione: anche i 401/403 prodotti dall'auth vengono restituiti come `application/problem+json`.

## Telemetria
Application Insights via `AddApplicationInsightsTelemetryWorkerService()` + `ConfigureFunctionsApplicationInsights()`; la connection string arriva dall'app setting `APPLICATIONINSIGHTS_CONNECTION_STRING`. Sampling configurato in `host.json`.

## Opzioni di configurazione (`Functions/Options/`)
Ogni classe implementa `IAppOptions` e si lega a una sezione di config; `OptionsRegistrationExtensions.AddOptions()` le registra per riflessione scansionando l'assembly Functions. Il Domain dipende dalle interfacce `I*Options`, con un adapter `IOptions<T>.Value` registrato in `Program.cs`.

| Classe | Sezione | Scopo |
|---|---|---|
| `AzureAdOptions` | `AzureAd` | Registrazione app Entra ID (Instance/TenantId/ClientId/Audience) usata da `JwtTokenValidator` |
| `AppOptions` | `App` | `FEUrl`, `AllowedLocalOrigins` (legacy, non usata) |
| `ExpenseExcelFileOptions` | `ExpenseTrackerExcel` | Indici colonna importer Excel legacy |
| `BbvaCsvOptions` / `SellaCsvOptions` / `SatisPayCsvOptions` / `TradeRepublicCsvOptions` | `BbvaCsv` / `SellaCsv` / `SatisPayCsv` / `TradeRepublicCsv` | Default e IBAN-to-account map per importer |
| `TransferMatchingOptions` | `TransferMatching` | `Rules[]` per il transfer matching |
| `KeyVaultOptions` | `KeyVault` | Non usata a runtime (wiring commentato in `Program.cs`) |
