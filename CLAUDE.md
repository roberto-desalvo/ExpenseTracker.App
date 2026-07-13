# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ExpenseTracker is a personal financial management web application for tracking incomes, expenses, and bank transfers. The backend is ASP.NET Core 10 and the frontend is React/TypeScript. Authentication uses Azure AD (Entra ID) via MSAL.

## Commands

### Backend (from repo root or `api/`)

```powershell
# Build solution
dotnet build ./api/RDS.ExpenseTracker.sln

# Run API (dev server at https://localhost:7120)
dotnet run --project ./api/RDS.ExpenseTracker.Api

# Run all tests
dotnet test ./api/RDS.ExpenseTracker.Tests/RDS.ExpenseTracker.Tests.csproj

# Run a single test (by name filter)
dotnet test ./api/RDS.ExpenseTracker.Tests/RDS.ExpenseTracker.Tests.csproj --filter "FullyQualifiedName~TestName"

# Add EF migration
dotnet ef migrations add <MigrationName> -p api/RDS.ExpenseTracker.Infrastructure -s api/RDS.ExpenseTracker.Api -o Migrations

# Apply migrations
dotnet ef database update -p api/RDS.ExpenseTracker.Infrastructure -s api/RDS.ExpenseTracker.Api
```

API docs available at `https://localhost:7120/scalar` (Scalar UI).

### Frontend (from `app/`)

```bash
npm install        # install dependencies
npm run dev        # dev server with HMR
npm run build      # production build
npm run lint       # ESLint
npm run preview    # preview production build
```

## Architecture

This project follows **Clean Architecture** with four layers:

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `RDS.ExpenseTracker.Domain` | Entities, interfaces, DTOs, custom exceptions |
| Application | `RDS.ExpenseTracker.Application` | Business logic, services, AutoMapper profiles |
| Infrastructure | `RDS.ExpenseTracker.Infrastructure` | EF Core DbContext, repositories, migrations |
| API | `RDS.ExpenseTracker.Api` | Controllers, middleware, DI wiring, configuration |

### Key Patterns

- **Repository Pattern**: Data access abstracted through interfaces defined in Domain, implemented in Infrastructure.
- **FluentResults**: Services return `Result<T>` rather than throwing exceptions for business logic errors. Controllers map failures to HTTP problem details via middleware.
- **AutoMapper**: DTO↔Entity mapping is in Application's `MappingProfile`.
- **Domain Exceptions**: Custom exception types (`BadRequestDomainException`, `NotFoundDomainException`, etc.) map 1:1 to HTTP status codes; the exception middleware handles the translation.

### Database

SQL Server via EF Core Code-First. Core entities:
- **Transaction** — primary record; optional FK to `Category` and `Transfer`; required FK to `Account`; `ExternalId` prevents duplicate imports
- **Account** — financial account
- **Category** — transaction category
- **Transfer** — links a pair of transactions representing an inter-account transfer

### Import Pipeline

`ImportController` accepts CSV or Excel uploads. Each bank has a dedicated import service (`BbvaCsvImportService`, `SellaCsvImportService`, `SatisPayCsvImportService`, `TradeRepublicCsvImportService`). After parsing, `TransferMatchingService` auto-links transfer pairs using configurable matching rules. PowerShell helper scripts in `scripts/` call these endpoints.

### Frontend

React 18 SPA built with Vite. State management uses React Context API (no Redux). UI uses MUI v6 + TailwindCSS. Pages: Landing (dashboard), Transactions, Categories, Accounts. All API calls go through service classes in `src/services/`.

Authentication is MSAL (Azure AD). The singleton `msalInstance` is initialized in `src/auth/msalInstance.ts`; the required scope is `api://[client-id]/access_as_user`.

## Environment Variables

Frontend (`.env` based on `.env.example`):
- `VITE_EXPENSE_TRACKER_API_BASE_URL`
- `VITE_MSAL_CLIENT_ID`
- `VITE_MSAL_TENANT_ID`
- `VITE_MSAL_API_SCOPE`

Backend: connection string and Azure AD settings in `appsettings.json` / environment overrides.

## Testing

Tests are in `RDS.ExpenseTracker.Tests` using **xUnit**, **FluentAssertions**, and **ArchUnitNET** (architecture rule tests). Coverage via Coverlet.
