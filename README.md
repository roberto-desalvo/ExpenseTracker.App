# Expense Tracker

Web app personale per la gestione di entrate, uscite e trasferimenti tra conti.
Demo disponibile https://thankful-island-04ae49a03.7.azurestaticapps.net/

## Stack
- **Backend**: Azure Functions (.NET 10, isolated worker), Clean Architecture, EF Core code-first su SQL Server.
- **Frontend**: React + TypeScript (Vite, MUI, TailwindCSS).
- **Autenticazione**: Azure AD (Entra ID) via MSAL.

## Funzionalità
- Dashboard e analisi dell'andamento di spese ed entrate per conto e categoria.
- Gestione movimenti, categorie e conti.
- Import di estratti conto CSV/Excel di BBVA, Sella, Satispay e Trade Republic, con deduplica dei movimenti.
- Riconoscimento automatico dei trasferimenti tra conti (anche tramite IBAN).
- Flusso di import parzialmente automatizzato tramite Logic App integrata con OneDrive.
- Modalità demo: accesso senza account Microsoft come utente di test, con dati fittizi.
- Seeding dei dati di test, richiamato una volta al giorno da Logic App per ripristinare i dati demo.

## Demo 
- https://thankful-island-04ae49a03.7.azurestaticapps.net/

## Avvio rapido
```powershell
# API (http://localhost:7120/api) — copiare prima local.settings.example.json in local.settings.json
cd api/RDS.ExpenseTracker.Functions; func start

# Frontend — configurare .env a partire da .env.example
cd app; npm install; npm run dev

# Test
dotnet test ./api/RDS.ExpenseTracker.Tests/RDS.ExpenseTracker.Tests.csproj
```

## Struttura
- `api/` — soluzione .NET (Domain, Application, Infrastructure, Functions, Tests)
- `app/` — frontend React
- `docs/` — documentazione tecnica e funzionale (`be/`, `fe/`)
- `tools/` — script PowerShell e collection Postman
