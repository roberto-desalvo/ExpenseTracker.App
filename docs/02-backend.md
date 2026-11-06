# 2. Backend

Il backend (`api/`) espone l'API REST usata dal frontend e dagli script di import, ed esegue gli import automatici e le attività pianificate. È un'app Azure Functions (isolated worker, .NET 10) organizzata in Clean Architecture.

## Layer della solution

```mermaid
flowchart LR
    F[Functions] --> A[Application] --> I[Infrastructure] --> D[Domain]
    F --> D
    A --> D
```

| Progetto | Contiene |
|---|---|
| `RDS.ExpenseTracker.Domain` | Entità, enum, DTO, interfacce di repository e servizi, errori ed eccezioni di dominio |
| `RDS.ExpenseTracker.Application` | Servizi di business, importer per banca, matching dei trasferimenti, generatori dei dati demo, profilo AutoMapper |
| `RDS.ExpenseTracker.Infrastructure` | `ExpenseTrackerContext` (EF Core), repository, entity configuration, migration, client Microsoft Graph |
| `RDS.ExpenseTracker.Functions` | Function HTTP/coda/timer, middleware, autenticazione, opzioni di configurazione, DI (`Program.cs`) |
| `RDS.ExpenseTracker.Tests` | Test xUnit |

La registrazione DI è per convenzione: `AddApplicationServices()` e `AddInfrastructureServices()` registrano tutti i servizi e repository come scoped; le classi di opzioni (`IAppOptions`) sono legate alle sezioni di configurazione per riflessione.

## Pipeline di una richiesta

1. **`RequestLoggingMiddleware`** — logga metodo, path e function.
2. **`ExceptionHandlingMiddleware`** — trasforma ogni `DomainException` in `ProblemDetails` (`application/problem+json`); il resto diventa 500.
3. **`AuthenticationMiddleware`** — valida il bearer token e applica gli attributi `[RequireRole]` / `[AllowAnonymousFunction]`.
4. **Function** — risolve l'utente con `ICurrentUserAccessor`, chiama il servizio e passa il `Result` a `FunctionBase.ExecuteAsync`, che lo converte in risposta HTTP.

### Autenticazione

| Modalità | Token | Dove è ammessa |
|---|---|---|
| Entra ID | JWT validato con i metadata OIDC del tenant (firma, issuer, audience, scadenza) | Sempre |
| Demo | JWT HS256 emesso da `POST /api/auth/demo` per l'utente `test@dev.local`, senza ruoli | Se `DemoAuth:Enabled=true` |
| Sviluppo | Header `Authorization: DevUser <nome>`, nessuna validazione | Solo in `Development` (`DevAuth:Enabled=true`; altrove l'host non parte) |

**Utente interno.** Al primo accesso con Entra ID l'utente viene creato dall'oid del token (*JIT provisioning*), con le sue categorie di default. Gli accessi demo e sviluppo cercano un utente esistente per email e non lo creano mai. Gli endpoint di import accettano anche token app-only, risolti tramite `Users.AppOid`.

**Admin.** `Users.IsAdmin` abilita le funzioni di amministrazione (oggi l'abbinamento dello storico Excel); il controllo è nel servizio, che risponde 403 agli altri utenti.

**Isolamento.** Ogni servizio riceve lo `userId` e i repository filtrano sui conti dell'utente: una risorsa di un altro utente risponde 404. Le uniche operazioni trasversali a tutti gli utenti sono il reset dei dati demo e la riconciliazione dei trasferimenti.

### Errori

I servizi restituiscono `Result`/`Result<T>` (FluentResults). Un errore porta un `DomainErrorKind` che diventa la corrispondente eccezione e quindi lo status:

| Kind | Status |
|---|---|
| BadRequest | 400 |
| Unauthorized | 401 |
| Forbidden | 403 |
| NotFound | 404 |
| Conflict | 409 |
| Validation | 422 |

## Endpoint

Prefisso `/api`. Tutti richiedono autenticazione salvo dove indicato. Una collection Postman con tutte le chiamate è in `tools/postman/` (da aggiornare insieme agli endpoint).

| Area | Endpoint | Scopo |
|---|---|---|
| Auth | `GET auth` | Id dell'utente interno |
| | `POST auth/demo` *(anonimo)* | Token per la modalità demo |
| Conti | `POST account/query`, `GET account/{id}` | Ricerca paginata, dettaglio |
| | `GET account/{id}/availability` | Saldo (somma dei movimenti) |
| | `POST account`, `PUT account` | Creazione (bulk), modifica (nome, IBAN, tipo di conto) |
| | `GET account-type` | Catalogo dei tipi di conto, con quelli importabili da file |
| Categorie | `POST category/query`, `GET category/{id}`, `GET category/default` | Ricerca, dettaglio, categoria predefinita |
| | `POST category`, `PUT category`, `DELETE category/{id}` | CRUD; l'eliminazione sposta i movimenti sulla predefinita |
| | `POST category/sync` | Ricategorizza i movimenti di un periodo con le parole chiave |
| Movimenti | `POST transaction/query` | Elenco paginato con filtri (anche `onlyTransfers`: collegati a un trasferimento o in Money transfers; `onlyIncompleteTransfers`: solo quelli con `transferIncomplete` o `transferMissing`) e ordinamento; ogni movimento riporta `transferId`, `transferIncomplete` (trasferimento senza il ramo di segno opposto) e `transferMissing` (Money transfers senza trasferimento) |
| | `GET transaction/{id}`, `GET transaction/latest`, `GET transaction/month-options` | Dettaglio, ultimo movimento, mesi disponibili |
| | `POST transaction`, `PUT transaction`, `DELETE transaction/{id}` | CRUD |
| | `POST transaction/bulk-delete`, `POST transaction/bulk-category` | Eliminazione e assegnazione della categoria su più movimenti (`ids`, `categoryId`; tutto o niente, max 1000) |
| | `POST transaction/{id}/split` | Divide il movimento in due con metà importo ciascuno e gli stessi dati (il centesimo dispari resta sull'originale, che mantiene l'`ExternalId`); non ammesso per i movimenti di un trasferimento con il ramo opposto; se il trasferimento è incompleto la nuova metà non vi è collegata |
| | `GET transaction/landing` | Aggregati della dashboard |
| | `POST transaction/series`, `POST transaction/stock` | Serie temporali: flussi per periodo, saldo medio per periodo |
| Trasferimenti | `GET transfer`, `GET transfer/{id}` | Elenco, dettaglio |
| | `POST transfer`, `PUT transfer/{id}`, `DELETE transfer/{id}` | CRUD (ogni trasferimento sono due movimenti collegati) |
| | `GET transaction/{id}/transfer-candidates` | Movimenti che completano il trasferimento incompleto di `{id}` (o di un movimento Money transfers senza trasferimento): altro conto, importo opposto, data nel mese di `{id}` o negli ultimi/primi 15 giorni del mese precedente/successivo, non già in un trasferimento completo |
| | `POST transaction/{id}/transfer-counterpart` | Body `{ counterpartId }`: collega il movimento al trasferimento di `{id}` e gli assegna la data di `{id}`; il suo trasferimento precedente, se resta vuoto, è eliminato. Se `{id}` non ha trasferimento si usa quello del corrispondente o se ne crea uno per entrambi. Con `{ newCounterpart: { accountId, description } }` crea invece il movimento mancante (altro conto, importo opposto, stessa data, categoria Money transfers; descrizione vuota = quella di `{id}`) |
| Utente | `GET user/me` | Profilo dell'utente corrente (email, `isDemo`, `isAdmin`) |
| Preferenze | `GET user/analytics-layout`, `PUT user/analytics-layout` | Layout della pagina Analisi (card visibili, ordine, larghezza); `null` se mai personalizzato |
| | `GET user/appearance`, `PUT user/appearance` | Preferenze di aspetto (`preset`, `mode`: light/dark/system, `nav`: sidebar/topbar); `null` se mai salvate |
| Demo | `POST demo/generate` | Rigenera i dati dell'utente demo (solo utenti con `IsDemo`) |
| Import dall'app | `POST imports` | Multipart `file` (CSV/XLSX, max 10 MB) + `accountId`: salva il file e lo accoda, risponde 202 con l'import `Queued` (non per l'utente demo) |
| | `POST imports/query` | Storico paginato degli import dell'utente (filtro opzionale per stato) |
| Import *(ruolo `Files.Sender`)* | `POST import/{banca}-csv`, `…-csv/stream`, `…-xlsx` | Import di un estratto conto (banca: `bbva`, `sella`, `satispay`, `traderepublic`) |
| | `POST import/excel`, `import/excel/base64`, `import/excel/stream` | Import del formato Excel storico |
| | `POST import/transfers/reconcile` | Collega a posteriori i trasferimenti (`dryRun` opzionale) |
| | `POST import/test-seed` | Rigenera l'utente di test con 5 anni di dati |
| Storico Excel *(admin)* | `POST legacy-match/sheets` | Multipart `file` (Spese.xlsx): fogli mensili del file |
| | `POST legacy-match/month` | Multipart `file` + `sheet`: righe del mese, movimenti a DB dello stesso mese (più gli ultimi 7 giorni del precedente e i primi 7 del successivo) e coppie suggerite |
| | `POST legacy-match/apply` | Salva descrizione e categoria sui movimenti abbinati e li marca (`LegacyMatchedAt`) |
| | `POST legacy-match/create` | Crea come nuovi movimenti (già marcati) le righe di Contanti e Hype |
| | `POST legacy-match/reset` | Body `{ year, month }`: annulla gli abbinamenti del mese (vedi sotto) |
| OneDrive | `POST onedrive/notifications` *(anonimo)* | Notifiche Microsoft Graph |

## Pipeline di import

```mermaid
flowchart TD
    app[ImportJob_Upload<br/>file caricato dall'app] --> blob[(Blob bank-imports)]
    app --> queued[(ImportLogs: Queued)]
    app --> queue[[Coda bank-file-import]]
    queue --> job[ImportJob_Process<br/>Processing → Completed / Failed]
    blob --> job
    job --> bfi
    http[ImportFunctions<br/>upload HTTP] --> bfi
    od[OneDrive_Sync<br/>file da /Transazioni/&lt;Banca&gt;] --> bfi
    bfi[IBankFileImportService<br/>conto dal tipo, sceglie l'importer, converte XLSX→CSV] --> imp
    imp[Importer della banca<br/>parse delle righe] --> dedup[Deduplica per ExternalId]
    dedup --> enrich[Categoria da parole chiave]
    enrich --> match[TransferMatchingService<br/>collega i giroconti]
    match --> save[(Salvataggio)]
    bfi --> log[(ImportLogs)]
```

- **Conto di destinazione** — si risolve dal tipo di conto: è il conto dell'utente il cui `AccountType` è elaborato da quella banca (al massimo uno per tipo). Se l'utente non ce l'ha, l'import fallisce: i conti non vengono più creati in automatico. Il formato Excel interno resta multi-conto (nomi dei conti nel file). Il sotto-conto Trade Republic per le righe TRADING si riconosce per nome (`TradeRepublicCsv:TradingAccountName`); se manca, le righe vanno sul conto Trade Republic.
- **Import asincrono dall'app** — `ImportJob_Upload` valida conto e file, salva il file nel container `bank-imports` dello storage dell'host, registra l'import come `Queued` e accoda l'id su `bank-file-import`. `ImportJob_Process` lo porta a `Processing`, esegue la stessa pipeline sul conto scelto e chiude il log come `Completed` o `Failed` (nessun retry sugli errori dell'import; un messaggio duplicato per un import già concluso è ignorato).
- **Importer per banca** — ognuno conosce il formato del proprio estratto conto e da dove ricavare l'`ExternalId` (BBVA: impronta SHA-256 dei campi; le altre: id nativo della riga).
- **Deduplica** — le righe con un `ExternalId` già presente tra i movimenti dell'utente sono scartate; un indice univoco sul database fa da rete di sicurezza.
- **Categorie** — assegnate cercando le parole chiave delle categorie dell'utente nella descrizione (vince la priorità più bassa); senza corrispondenza si usa la categoria predefinita.
- **Trasferimenti** — riconosciuti in tre modi: IBAN della controparte uguale all'IBAN di un altro conto dell'utente; regole configurate in `TransferMatching:Rules` (tipo di conto + testo della descrizione sui due lati); a posteriori, con la riconciliazione, anche per categoria di sistema `MoneyTransfers` con importo opposto entro 3 giorni.
- **Tracciamento** — ogni import (riuscito o fallito) scrive una riga in `ImportLogs` con stato, periodo coperto, movimenti nuovi, duplicati scartati ed eventuali errori; gli import sincroni (HTTP, OneDrive) nascono già conclusi.
- **OneDrive** — un file importato viene spostato in `<Banca>/Importati`, uno fallito in `<Banca>/Errori`. Un lock sul database impedisce due sincronizzazioni contemporanee; la posizione della delta query è salvata in `OneDriveSyncStates`.

Gli script PowerShell in `tools/scripts/` chiamano gli endpoint di import.

## Abbinamento dello storico Excel

Il vecchio file delle spese (`Spese.xlsx`, mai versionato) ha un foglio per mese (`Gennaio2026`, `Marzo 2022`…) con le righe in A–E (Data, Ragione, Uscite, Entrate, Fondo = conto) e, sotto la cella `Parametri importanti`, le categorie: il nome in colonna G e in colonna H una formula `SUMIF(B:B,"*parola*",C:C)+…`. `LegacySpreadsheetReader` legge valori e formule con OpenXml (ExcelDataReader non espone le formule); il file non viene salvato, il client lo rimanda a ogni richiesta.

- **Righe** — escluse quelle senza data o importo e gli `aggiustamento`; i giroconti (colonne J–N) non sono letti. Importo = Entrate − Uscite.
- **Categoria Excel** — la prima del foglio con una parola (termine sommato della formula) contenuta nella Ragione, preferendo le parole della colonna coerente con il segno. Suggerita la categoria dell'utente con lo stesso nome; in mancanza quella trovata dai tag sulla descrizione della riga (`CategoryTagMatcher`, senza categoria predefinita), altrimenti nessuna (da determinare).
- **Contanti e Hype** — conti senza import automatico (riconosciuti dal tipo del conto o dal nome del Fondo): le loro righe non vengono abbinate ma create come nuovi movimenti (`legacy-match/create`).
- **Suggerimenti** (`LegacyMatchSuggester`) — conto risolto per nome del conto o del suo tipo; coppie 1:1 con stesso conto e importo entro ±0,02 €, preferendo la data più vicina; esclusi i movimenti già abbinati.
- **Salvataggio** — il movimento prende descrizione e categoria scelte nella pagina (proposte "descrizione DB - descrizione Excel" e categoria suggerita) e `LegacyMatchedAt`; al primo abbinamento la descrizione bancaria resta in `LegacyOriginalDescription`. I movimenti creati hanno `Source = LegacyImport`.
- **Ripristino del mese** (`legacy-match/reset`) — sui movimenti del mese con `LegacyMatchedAt`: elimina quelli con `Source = LegacyImport`; agli altri toglie l'abbinamento, rimette la descrizione originale e la categoria di default degli import (trasferimenti collegati in "Trasferimenti", altrimenti la regola sui tag della descrizione o la categoria predefinita).
