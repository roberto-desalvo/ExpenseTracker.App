# Expense Tracker

PWA personale per la gestione di entrate, uscite e trasferimenti tra conti.
Demo disponibile www.expensetracker.lol

## Stack
- **Backend**: Azure Functions (.NET 10, isolated worker), Clean Architecture, EF Core code-first su SQL Server.
- **Frontend**: Angular + TypeScript (signals, Angular Material, ECharts).
- **Autenticazione**: Azure AD (Entra ID) via MSAL.

## Funzionalità
- Dashboard e analisi dell'andamento di spese ed entrate per conto e categoria.
- Gestione movimenti, categorie e conti.
- Import di estratti conto CSV/Excel di BBVA, Sella, Satispay e Trade Republic, con deduplica dei movimenti.
- Riconoscimento automatico dei trasferimenti tra conti (anche tramite IBAN).
- Import automatico degli estratti conto salvati in una cartella OneDrive (notifiche Microsoft Graph → Azure Function).
- Modalità demo: accesso senza account Microsoft come utente di test, con dati fittizi.
- Seeding dei dati di test, eseguito ogni giorno da una timer function per ripristinare i dati demo.

## Demo 
- www.expensetracker.lol

## Avvio rapido
```powershell
# API (http://localhost:7120/api) — copiare prima local.settings.example.json in local.settings.json
cd api/RDS.ExpenseTracker.Functions; func start

# Frontend (http://localhost:5173) — configurare web/.env.local a partire da web/.env.example
cd web; npm install; npm start

# Test
dotnet test ./api/RDS.ExpenseTracker.Tests/RDS.ExpenseTracker.Tests.csproj
```

## Struttura
- `api/` — soluzione .NET (Domain, Application, Infrastructure, Functions, Tests)
- `web/` — frontend Angular
- `app/` — vecchio frontend React (non più pubblicato, da rimuovere)
- `docs/` — documentazione (parti da [`docs/README.md`](docs/README.md))
- `tools/` — script PowerShell e collection Postman
