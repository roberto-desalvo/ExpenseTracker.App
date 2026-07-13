# 01 - Overview and boundaries (Tecnico)

## Obiettivo
Descrivere stack, architettura e confini del BE.

## Stack
- ASP.NET Core 10 (Web API), C#
- EF Core 10 (Code First) su SQL Server
- Auth: Azure AD (Entra ID) via `Microsoft.Identity.Web` (JWT bearer)
- FluentResults per gli esiti applicativi, AutoMapper per il mapping DTO↔Entity

## Solution structure (Clean Architecture)
Dipendenza a senso unico `Api → Application → Infrastructure → Domain` (Infrastructure e Application referenziano anche Domain direttamente):

| Progetto | Responsabilità |
|---|---|
| `RDS.ExpenseTracker.Domain` | Entità, enum, DTO, interfacce repository/service, eccezioni di dominio |
| `RDS.ExpenseTracker.Application` | Servizi di business, importer CSV/Excel, transfer matching, AutoMapper profile |
| `RDS.ExpenseTracker.Infrastructure` | `DbContext`, repository EF Core, entity configurations, migrations |
| `RDS.ExpenseTracker.Api` | Controller, middleware, DI wiring, opzioni di configurazione |
| `RDS.ExpenseTracker.Tests` | Test xUnit |

Tutti i progetti puntano a `net10.0`.

## Confine BE
- Espone REST API per account, categorie, transazioni, trasferimenti e import bancari.
- Possiede tutte le regole di dominio persistenti (dedup, categorizzazione, matching trasferimenti).
- Non include UI: il consumo è delegato al frontend (`app/`).

## Entry point
- `api/RDS.ExpenseTracker.Api/Program.cs`

## Comandi principali
```powershell
dotnet build ./api/RDS.ExpenseTracker.sln
dotnet run --project ./api/RDS.ExpenseTracker.Api
dotnet test ./api/RDS.ExpenseTracker.Tests/RDS.ExpenseTracker.Tests.csproj
```
Docs interattive: `https://localhost:7120/scalar` (Scalar UI, basata su `AddOpenApi`/`MapOpenApi`, non Swashbuckle).
