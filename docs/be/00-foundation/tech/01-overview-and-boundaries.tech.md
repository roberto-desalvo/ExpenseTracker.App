# 01 - Overview and boundaries (Tecnico)

## Obiettivo
Descrivere stack, architettura e confini del BE.

## Stack
- Azure Functions v4 (isolated worker, .NET 10) con integrazione ASP.NET Core, C#
- EF Core 10 (Code First) su SQL Server
- Auth: Azure AD (Entra ID), JWT bearer validato da un middleware del worker (`Microsoft.IdentityModel.*`)
- FluentResults per gli esiti applicativi, AutoMapper per il mapping DTO↔Entity

## Solution structure (Clean Architecture)
Dipendenza a senso unico `Functions → Application → Infrastructure → Domain` (Infrastructure e Application referenziano anche Domain direttamente):

| Progetto | Responsabilità |
|---|---|
| `RDS.ExpenseTracker.Domain` | Entità, enum, DTO, interfacce repository/service, eccezioni di dominio |
| `RDS.ExpenseTracker.Application` | Servizi di business, importer CSV/Excel, transfer matching, AutoMapper profile |
| `RDS.ExpenseTracker.Infrastructure` | `DbContext`, repository EF Core, entity configurations, migrations |
| `RDS.ExpenseTracker.Functions` | HTTP-triggered functions, middleware worker, auth JWT, DI wiring, opzioni di configurazione |
| `RDS.ExpenseTracker.Tests` | Test xUnit |

Tutti i progetti puntano a `net10.0`.

## Confine BE
- Espone REST API per account, categorie, transazioni, trasferimenti e import bancari.
- Possiede tutte le regole di dominio persistenti (dedup, categorizzazione, matching trasferimenti).
- Non include UI: il consumo è delegato al frontend (`app/`).

## Entry point
- `api/RDS.ExpenseTracker.Functions/Program.cs` (host worker) + `host.json` (configurazione host Functions)

## Comandi principali
```powershell
dotnet build ./api/RDS.ExpenseTracker.sln
cd ./api/RDS.ExpenseTracker.Functions; func start   # Azure Functions Core Tools v4, http://localhost:7120
dotnet test ./api/RDS.ExpenseTracker.Tests/RDS.ExpenseTracker.Tests.csproj
```
Non è più esposta una UI OpenAPI/Scalar: l'elenco degli endpoint è in [03-api-surface-and-routing](../../01-platform/tech/03-api-surface-and-routing.tech.md).
