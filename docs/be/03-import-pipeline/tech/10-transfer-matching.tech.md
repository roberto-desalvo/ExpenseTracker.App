# 10 - Transfer matching (Tecnico)

## Servizio
`TransferMatchingService` (`Application/Services/`), implementa `ITransferMatchingService`. Usato da Sella, Satispay e Trade Republic (non BBVA).

## Due meccanismi

### IBAN-based
Se l'IBAN controparte di una riga risolve, via `IbanToAccountMap` (config per-importer), a un account interno noto, la riga diventa direttamente una `Transfer` con due `Transaction` leg (es. "Ricarica Satispay" da bonifico bancario).

### Rule-based cross-source (`ITransferMatchingOptions.Rules` / `TransferMatchRule`)
Configurato in `TransferMatching:Rules` (`appsettings.json`), es. "Trade Republic Sepa Direct Debit → Satispay Ricarica" e "Sella Satispay Europe → Satispay Ricarica".

- `IsTransferCandidate(accountName, description)` — vero se l'account+descrizione compare su uno dei due lati di una regola configurata (`AccountName1`/`DescriptionPattern1` ↔ `AccountName2`/`DescriptionPattern2`, match `Contains`/`StartsWith` via `DescriptionMatchMode`). Non richiede `userId`: fa solo pattern-matching sulle regole configurate, senza toccare il database.
- `TryMatchAsync(accountName, description, signedAmount, date, userId, consumedCandidateIds)` — calcola importo/data attesi sul lato opposto (offset configurabile, default 1 giorno), risolve l'account controparte tra quelli **dell'utente corrente** (`IAccountRepository.GetAccounts(userId)`, metodo privato `ClaimCandidateAsync`, anch'esso ora parametrizzato su `userId`), interroga `ITransactionRepository.GetUnlinkedTransferCandidates` (stesso account, `TransferId == null`, importo esatto, data esatta, descrizione `Contains`), esclude gli id già consumati nella stessa import call, e reclama il match più vecchio (`OrderBy(Date).ThenBy(Id)`).
  - **Fix di isolamento dati**: in precedenza la ricerca dell'account controparte non era filtrata per utente e scorreva tutti gli account di tutti gli utenti, con rischio di far matchare (e quindi collegare come `Transfer`) transazioni appartenenti a persone diverse. Ora `GetAccounts(userId)` limita la ricerca ai soli conti dell'utente che ha effettuato l'import.
- Le righe candidate vengono processate in ordine cronologico crescente all'interno di una singola chiamata di import; gli id consumati sono tracciati in un `HashSet<int>` per evitare che più righe dello stesso importo reclamino la stessa controparte.
- Se trovato un match, viene creata una `Transfer` che collega retroattivamente la nuova transazione a quella candidata già esistente (ora linkata). Se non trovato, la riga resta una `Transaction` semplice con categoria `MoneyTransfers` (non collegata), disponibile per essere abbinata da un futuro import della sorgente controparte.

## Config di esempio (`appsettings.json`, sezione `TransferMatching`)
```json
{
  "AccountName1": "Trade Republic",
  "DescriptionPattern1": "Sepa Direct Debit transfer to Satispay Europe S.A.",
  "AccountName2": "Satispay",
  "DescriptionPattern2": "Ricarica Satispay"
}
```
