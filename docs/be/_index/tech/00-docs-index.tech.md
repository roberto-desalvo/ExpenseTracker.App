# Backend Handbook (Tecnico)

## Scopo
Indice tecnico della documentazione modulare BE.

## Come usare questi documenti
1. Parti da `01-overview-and-boundaries`.
2. Segui l'ordine numerico fino a `12-cicd-and-configuration`.
3. Per ogni modulo tecnico esiste la versione non tecnica con lo stesso ID.

## Mappa moduli (tech -> non-tech)
- 01 Overview and boundaries
- 02 Domain model
- 03 API surface and routing
- 04 Auth and request pipeline
- 05 Error handling
- 06 Database and EF Core
- 07 Application services and mapping
- 08 Import overview and dedup
- 09 Bank-specific importers
- 10 Transfer matching
- 11 Testing
- 12 CI/CD and configuration

## Sorgenti principali
- `api/RDS.ExpenseTracker.Domain/*`
- `api/RDS.ExpenseTracker.Application/*`
- `api/RDS.ExpenseTracker.Infrastructure/*`
- `api/RDS.ExpenseTracker.Api/*`
- `api/RDS.ExpenseTracker.Tests/*`
- `api/.github/workflows/*`

## Regole di manutenzione
- Quando cambia una feature BE, aggiornare sia il file `.tech.md` sia il file `.non-tech.md` corrispondente.
- Mantenere stesso ID e stesso titolo logico tra le due versioni.
- Se cambia la struttura dei moduli, aggiornare anche questo indice e quello non tecnico.
