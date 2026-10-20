# 10 - Transfer matching (Tecnico)

## Servizio
`TransferMatchingService` (`Application/Services/`), implementa `ITransferMatchingService`. Usato da Sella, Satispay e Trade Republic (non BBVA).

## Due meccanismi in fase di import

### IBAN-based
Se l'IBAN controparte di una riga corrisponde all'`Account.Iban` di un altro conto **dell'utente** (`IbanUtilities.FindAccount`, confronto su IBAN normalizzato: maiuscolo, senza spazi), la riga diventa direttamente una `Transfer` con due `Transaction` leg (es. "Ricarica Satispay" da bonifico bancario). Un IBAN che punta al conto stesso dell'importer viene ignorato. Gli IBAN si impostano sui conti (colonna nullable `Accounts.Iban`, vedi [06-database-and-ef-core](../../02-data-persistence/tech/06-database-and-ef-core.tech.md)); la vecchia mappa `IbanToAccountMap` nelle opzioni degli importer è stata rimossa, perché i valori vivevano solo nelle app settings dell'App Service e sono andati persi con la migrazione a Functions.

### Rule-based cross-source (`ITransferMatchingOptions.Rules` / `TransferMatchRule`)
Configurato in `TransferMatching:Rules` (`appsettings.json`), es. "Trade Republic Sepa Direct Debit → Satispay Ricarica" e "Sella Satispay Europe → Satispay Ricarica".

- `IsTransferCandidate(accountName, description)` — vero se l'account+descrizione compare su uno dei due lati di una regola configurata (`AccountName1`/`DescriptionPattern1` ↔ `AccountName2`/`DescriptionPattern2`, match `Contains`/`StartsWith` via `DescriptionMatchMode`). Non richiede `userId`: fa solo pattern-matching sulle regole configurate, senza toccare il database.
- `TryMatchAsync(accountName, description, signedAmount, date, userId, consumedCandidateIds)` — calcola importo/data attesi sul lato opposto (offset configurabile, default 1 giorno), risolve l'account controparte tra quelli **dell'utente corrente** (`IAccountRepository.GetAccounts(userId)`, metodo privato `ClaimCandidateAsync`, anch'esso ora parametrizzato su `userId`), interroga `ITransactionRepository.GetUnlinkedTransferCandidates` (stesso account, `TransferId == null`, importo esatto, data esatta, descrizione `Contains`), esclude gli id già consumati nella stessa import call, e reclama il match più vecchio (`OrderBy(Date).ThenBy(Id)`).
  - **Fix di isolamento dati**: in precedenza la ricerca dell'account controparte non era filtrata per utente e scorreva tutti gli account di tutti gli utenti, con rischio di far matchare (e quindi collegare come `Transfer`) transazioni appartenenti a persone diverse. Ora `GetAccounts(userId)` limita la ricerca ai soli conti dell'utente che ha effettuato l'import.
- Le righe candidate vengono processate in ordine cronologico crescente all'interno di una singola chiamata di import; gli id consumati sono tracciati in un `HashSet<int>` per evitare che più righe dello stesso importo reclamino la stessa controparte.
- Se trovato un match, viene creata una `Transfer` che collega retroattivamente la nuova transazione a quella candidata già esistente (ora linkata). Se non trovato, la riga resta una `Transaction` semplice con categoria `MoneyTransfers` (non collegata), disponibile per essere abbinata da un futuro import della sorgente controparte.

## Riconciliazione a posteriori (`TransferReconciliationService`)
`Application/Services/TransferReconciliationService.cs`, implementa `ITransferReconciliationService`; esposto da `POST /api/import/transfers/reconcile` (vedi [03-api-surface-and-routing](../../01-platform/tech/03-api-surface-and-routing.tech.md)). Serve a sistemare i movimenti già importati le cui due metà non sono state collegate (es. import fatti senza IBAN sui conti, o con la controparte importata con date sfasate).

- Opera su **tutti gli utenti**, senza dipendere dall'identità del chiamante. Input: `fromDate`/`toDate` opzionali (un `toDate` senza orario include tutto il giorno), `dryRun`.
- Accesso ai dati tramite `ITransferReconciliationRepository` (`Infrastructure/Repositories/TransferReconciliationRepository.cs`):
  - `GetAllAccounts()` — tutti i conti, senza tracking; il servizio li raggruppa per `UserId` e lavora un utente alla volta (i trasferimenti sono sempre tra conti dello stesso utente, e la memoria resta proporzionale ai movimenti di un solo utente).
  - `GetUnlinkedTransactions(accountIds, fromDate, toDate)` — una query per utente, `AsNoTracking`, proiettata sui soli campi del matching (`Id`, `AccountId`, `Amount`, `Date`, `Description`, `CategoryId`), filtro `TransferId == null`, data valorizzata, importo ≠ 0, ordinata per `Date`, `Id`.
  - `LinkTransfers(pairs, categoryId)` — per ogni coppia aggiunge un `Transfer` e aggancia i due movimenti come stub (`Attach` di `new Transaction { Id }`, senza caricarli), così l'UPDATE tocca solo `TransferId`, `CategoryId` e `UpdatedOn`. Salva a blocchi di 500 coppie: un `SaveChanges` per blocco (insert dei `Transfer` e update dei movimenti nella stessa transazione, compatibile con la retry strategy), poi `ChangeTracker.Clear()`.
- Matching interamente in memoria (classe privata `UserMatcher`) su un indice `(AccountId, Amount)` → movimenti in ordine di data: la controparte di un movimento si cerca solo nei bucket `(altro conto, -Amount)`, quindi il costo è lineare nel numero di movimenti. Ogni movimento può stare in una sola coppia (`HashSet` dei consumati). Tre passate, in ordine `Date`, `Id`:
  1. **Rule** — `ITransferMatchingService.GetCounterpartRules(accountName, description)` restituisce in memoria il lato opposto delle regole `TransferMatching` che riconoscono il movimento (`TransferRuleCounterpart`: conto, pattern, match mode); la controparte deve stare su quel conto (risolto per nome tra i conti dell'utente), con la data a esattamente `TransferMatchingService.InboundOffsetDays` (1) giorno di distanza come all'import, e descrizione che soddisfa il pattern. Vince la più vecchia.
  2. **Iban** — la descrizione contiene (`IbanUtilities.ExtractAll`) l'IBAN di un altro conto dell'utente; la controparte è su quel conto.
  3. **Category** — il movimento è in categoria `MoneyTransfers` (tipicamente una metà rimasta senza controparte all'import); la controparte può stare su qualsiasi altro conto, preferendo quelle già in `MoneyTransfers`. Se due candidati hanno lo stesso punteggio (categoria e distanza in giorni) il movimento resta scollegato e viene contato in `AmbiguousSkipped`.
  - Per Iban e Category la controparte deve avere importo esattamente opposto, conto diverso e data entro **3 giorni** (`DateToleranceDays`); vince la data più vicina, poi l'`Id` più basso.
- Per ogni coppia crea una `Transfer`, collega i due movimenti, li porta in categoria `MoneyTransfers` e aggiorna `UpdatedOn` (scrittura per utente, al termine del suo matching). Con `dryRun=true` non scrive nulla.
- Output `TransferReconciliationResultDto`: `DryRun`, `UsersProcessed`, `ExaminedTransactions`, `LinkedTransfers`, `MatchedByRule`/`MatchedByIban`/`MatchedByCategory`, `AmbiguousSkipped`, `Pairs` (utente, id dei due movimenti, conti, importo, date, `Method`).
- Il `description` matching delle regole è condiviso tra import e riconciliazione: `TransferMatchRule.MatchesDescription` (`Domain/Common/TransferMatchRule.cs`).

## Config di esempio (`appsettings.json`, sezione `TransferMatching`)
```json
{
  "AccountName1": "Trade Republic",
  "DescriptionPattern1": "Sepa Direct Debit transfer to Satispay Europe S.A.",
  "AccountName2": "Satispay",
  "DescriptionPattern2": "Ricarica Satispay"
}
```
