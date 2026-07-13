# 03 - API surface and routing (Tecnico)

## Base
Tutti i controller (tranne `ImportController`) ereditano da `ApiControllerBase`, che scompone `FluentResults.Result` in risposte HTTP (vedi [05-error-handling](05-error-handling.tech.md)). Tutte le route richiedono autorizzazione (`MapControllers().RequireAuthorization()`).

## `AuthController` — `api/Auth`
- `GET /` — profilo utente autenticato (id/email/nome da claim JWT).

## `AccountController` — `api/Account`
- `POST /query` — ricerca paginata account.
- `GET /{id}` — account singolo.
- `GET /{id}/availability` — saldo calcolato.
- `POST /` — creazione bulk.
- `PUT /` — update.

## `CategoryController` — `api/Category`
- `POST /query` — ricerca paginata categorie.
- `GET /{id}` — categoria singola.
- `GET /default` — categoria di default.
- `POST /` — creazione bulk.
- `PUT /` — update.
- `DELETE /{id}` — elimina (riassegna prima le transazioni alla categoria di default).

## `TransactionController` — `api/Transaction`
- `POST /query` — ricerca paginata con totali income/outcome/net.
- `GET /month-options` — mesi disponibili per i filtri UI.
- `GET /landing` — aggregazione dashboard (saldi, riepilogo categorie, serie net-worth).
- `GET /{id}` — transazione singola.
- `GET /latest` — transazione più recente.
- `POST /` — crea.
- `PUT /` — aggiorna.
- `DELETE /{id}` — elimina.
- `POST /series` — time-series per periodo (somme).
- `POST /stock` — time-series cumulativa.

## `TransferController` — `api/Transfer`
- `GET /` — elenco trasferimenti.
- `GET /{id}` — trasferimento singolo.
- `POST /` — crea (due leg collegate).
- `PUT /{id}` — aggiorna (ricrea le due leg).
- `DELETE /{id}` — elimina trasferimento + leg.

## `DemoController` — `api/Demo`
- `POST /generate` — rigenera da zero i dati demo (account, transazioni, trasferimenti) per l'utente autenticato. Fallisce con 403 se l'utente corrente non ha `IsDemo = true` sul record `User`. Vedi [07-application-services-and-mapping](../../02-data-persistence/tech/07-application-services-and-mapping.tech.md) per la logica di generazione.

## `ImportController` (`[Authorize(Roles = "Files.Sender")]`) — `api/Import`
Vedi [08-import-overview-and-dedup](../../03-import-pipeline/tech/08-import-overview-and-dedup.tech.md).
- `POST /excel`, `/excel/base64`, `/excel/stream` — import Excel legacy multi-sheet.
- `POST /bbva-csv`, `/sella-csv`, `/satispay-csv`, `/traderepublic-csv` — import CSV bancario (multipart, `.csv`).
- `POST /{bank}-csv/stream` — variante raw octet-stream.
- `POST /{bank}-xlsx` — variante `.xlsx` raw, convertita in CSV in-memory prima dell'import.

Tutti gli endpoint di import accettano il flag opzionale `?importAll`.
