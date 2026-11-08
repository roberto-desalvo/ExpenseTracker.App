# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ExpenseTracker is a personal financial management web application for tracking incomes, expenses, and bank transfers. The backend is an Azure Functions app (isolated worker, .NET 10) and the frontend is Angular/TypeScript (`web/`). Authentication uses Azure AD (Entra ID) via MSAL.

## Commands

### Backend (from repo root or `api/`)

```powershell
# Build solution
dotnet build ./api/RDS.ExpenseTracker.sln

# Run API locally (Azure Functions Core Tools v4, http://localhost:7120/api); first copy local.settings.example.json to local.settings.json
cd ./api/RDS.ExpenseTracker.Functions; func start

# Run all tests
dotnet test ./api/RDS.ExpenseTracker.Tests/RDS.ExpenseTracker.Tests.csproj

# Run a single test (by name filter)
dotnet test ./api/RDS.ExpenseTracker.Tests/RDS.ExpenseTracker.Tests.csproj --filter "FullyQualifiedName~TestName"

# Add EF migration
dotnet ef migrations add <MigrationName> -p api/RDS.ExpenseTracker.Infrastructure -s api/RDS.ExpenseTracker.Functions -o Migrations

# Apply migrations
dotnet ef database update -p api/RDS.ExpenseTracker.Infrastructure -s api/RDS.ExpenseTracker.Functions
```

There is no OpenAPI/Scalar UI; the endpoint list is in `docs/02-backend.md`.

### Infrastructure (Bicep, `infra/`)

```powershell
# Build/lint the templates
az bicep build --file ./infra/main.bicep; az bicep lint --file ./infra/main.bicep

# Preview / deploy: prod.bicepparam reads env vars from infra/.env (gitignored, template infra/.env.example), loaded by the script below
. ./infra/scripts/load-env.ps1
az deployment group what-if -g <resource-group> --parameters ./infra/parameters/prod.bicepparam

# One-time bootstrap (admin, local): resource group, GitHub OIDC deploy identity, Graph permission, RBAC, Files.Sender assignments
az deployment sub create -l westeurope --parameters ./infra/bootstrap/bootstrap.bicepparam
# ...or all in one (load .env + bootstrap + GitHub environment secrets/variables via gh); supports -WhatIf, -SkipBootstrap, -SkipGitHub
./infra/scripts/bootstrap.ps1

# One-time OneDrive consent for the automatic import (device code; refresh token -> Key Vault, then creates the Graph subscription)
./infra/scripts/connect-onedrive.ps1
```

Entra ID app registrations (API + SPA) are created by `infra/modules/entra.bicep` via the Microsoft Graph Bicep extension (`infra/bicepconfig.json`).

CI: `.github/workflows/deploy-infra.yml` (validate → what-if on PRs → deploy on `main`). Details in `infra/README.md` and `docs/05-operazioni.md`.

### Frontend (from `web/`)

```bash
npm install        # install dependencies
npm start          # ng serve on http://localhost:5173 (generates src/environments/env.generated.ts first)
npm run build      # production build -> dist/web/browser
npm run lint       # angular-eslint
npm test           # unit tests (Vitest, single run)
```

`app/` contains the legacy React frontend, kept only until the Angular app is in production; it is no longer deployed or documented.

## Architecture

This project follows **Clean Architecture** with four layers:

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `RDS.ExpenseTracker.Domain` | Entities, interfaces, DTOs, custom exceptions |
| Application | `RDS.ExpenseTracker.Application` | Business logic, services, AutoMapper profiles |
| Infrastructure | `RDS.ExpenseTracker.Infrastructure` | EF Core DbContext, repositories, migrations |
| API | `RDS.ExpenseTracker.Functions` | HTTP-triggered functions, worker middleware (logging, exceptions, JWT auth), DI wiring, configuration |

### Key Patterns

- **Repository Pattern**: Data access abstracted through interfaces defined in Domain, implemented in Infrastructure.
- **FluentResults**: Services return `Result<T>` rather than throwing exceptions for business logic errors. Functions (via `FunctionBase.ExecuteAsync`) map failures to domain exceptions, which the worker `ExceptionHandlingMiddleware` turns into HTTP problem details.
- **AutoMapper**: DTO↔Entity mapping is in Application's `MappingProfile`.
- **Domain Exceptions**: Custom exception types (`BadRequestDomainException`, `NotFoundDomainException`, etc.) map 1:1 to HTTP status codes; the exception middleware handles the translation.

### Database

SQL Server via EF Core Code-First. Core entities:
- **Transaction** — primary record; optional FK to `Category` and `Transfer`; required FK to `Account`; `ExternalId` prevents duplicate imports
- **Account** — financial account
- **Category** — transaction category
- **Transfer** — links a pair of transactions representing an inter-account transfer

### Import Pipeline

`ImportFunctions` accepts CSV or Excel uploads; it, the automatic OneDrive import and the asynchronous in-app import all go through `IBankFileImportService` (target account resolved from the account's `AccountType`, routing per bank, XLSX→CSV conversion, `ImportLogs`). Each bank has a dedicated import service (`BbvaCsvImportService`, `SellaCsvImportService`, `SatisPayCsvImportService`, `TradeRepublicCsvImportService`) that receives the target account. After parsing, `TransferMatchingService` auto-links transfer pairs using configurable matching rules (by account type). PowerShell helper scripts in `tools/scripts/` call these endpoints.

`ImportJobFunctions` handle the in-app import: `POST imports` stores the file in the `bank-imports` blob container, records a `Queued` `ImportLog` and enqueues it on `bank-file-import`; `ImportJob_Process` runs the pipeline via `ImportJobService` and sets the log to `Completed`/`Failed`.

`OneDriveImportFunctions` replace the former Logic App: Microsoft Graph change notifications on the OneDrive folder (`/Transazioni/<Bank>/file`) → queue `onedrive-sync` → `OneDriveImportService` (delta query, import, move to `Importati`/`Errori`); a daily timer renews the Graph subscription. The drive is a personal OneDrive, so Graph is called with a delegated refresh token kept in Key Vault (`OneDriveTokenProvider`).

### Frontend

Angular 22 SPA (standalone components, signals, zoneless) in `web/`. State lives in root-provided signal stores in `src/app/core/state/` (no NgRx). UI uses Angular Material + CDK with SCSS; charts use ECharts via `ngx-echarts`. Pages (lazy routes in `src/app/app.routes.ts`): Dashboard (`/`), Movimenti (`/movimenti`), Analisi (`/analisi`), Pianificazione (`/pianificazione`), Impostazioni (`/impostazioni`: Categorie, Conti, Aspetto). Visual styles are switchable presets (`src/app/core/theme/stylePresets.ts`); `ThemeService` writes the active tokens as CSS variables (`--app-*`, `--mat-sys-*`) on `:root`, and components use `var(--app-…)` or `ThemeService.tokens()`. All API calls go through `ApiClient` + the `*.api.ts` services in `src/app/core/api/`; auth header and error notifications are HTTP interceptors.

Authentication is MSAL (Azure AD) via `@azure/msal-browser` directly: the singleton `msalInstance` is in `src/app/core/auth/msalInstance.ts` and `AuthService` initializes it in an app initializer; the required scope is `api://[client-id]/access_as_user`.

## Environment Variables

Frontend (`web/.env.local` based on `web/.env.example`; the `VITE_` prefix is historical; in CI `deploy-frontend.yml` fills them from the infra deployment outputs). `scripts/write-env.mjs` merges the `.env*` files with the process environment (which wins) into the gitignored `src/environments/env.generated.ts`, read by `src/app/core/config/env.ts`:
- `VITE_EXPENSE_TRACKER_API_BASE_URL`
- `VITE_MSAL_CLIENT_ID`
- `VITE_MSAL_TENANT_ID`
- `VITE_MSAL_API_SCOPE`
- optional: `VITE_DEV_AUTH_BYPASS` (`false` to use MSAL with `npm start`), `VITE_DEMO_LOGIN_ENABLED`

Backend: connection string and Azure AD settings in `appsettings.json` / environment overrides.

## Testing

Tests are in `RDS.ExpenseTracker.Tests` using **xUnit**, **FluentAssertions**, and **ArchUnitNET** (architecture rule tests). Coverage via Coverlet.

Frontend unit tests are `*.spec.ts` files next to the code in `web/src/app/`, run with Vitest through `ng test` (`npm test` from `web/`).

## Documentation

Concise documentation (in Italian) lives in `docs/`, a single version for all readers. Start at `docs/README.md`:

- `01-architettura.md` — main technical choices, system components (Mermaid diagram)
- `02-backend.md` — solution layers, request pipeline and auth, endpoint list, import pipeline
- `03-frontend.md` — UI organization (pages, shell), code structure, state, auth modes, theming
- `04-dati.md` — data schema (Mermaid ER diagram), constraints, data rules, migrations
- `05-operazioni.md` — infrastructure, CI/CD workflows, configuration, local run, tests

**Maintenance rule — always follow this after making a code change:** when a change alters a technical choice, a system component, an endpoint, a UI page or its organization, the data schema, or configuration, update the matching section of the relevant `docs/` file in the same change. Keep the docs short: describe choices and structure, not implementation details derivable from the code, and never the history of changes (git has it).
