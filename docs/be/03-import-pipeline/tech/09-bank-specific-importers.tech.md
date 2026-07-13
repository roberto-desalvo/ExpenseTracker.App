# 09 - Bank-specific importers (Tecnico)

Tutti implementano `I*CsvImportService` (`Domain/Services/`) e sono configurati via `I*CsvOptions` (`Domain/Common/`, bind in `Api/Options/`). Ciascuno riceve `int userId` come parametro di `ImportFromCsvAsync` e lo usa nel proprio metodo privato `EnsureAccountsExistAsync(names, userId)`, che cerca/crea gli `Account` richiesti filtrati per quello specifico utente (`IAccountRepository.GetAccounts(userId)` / `AddAccounts` con `new Account(0, name, userId)`) — vedi [08-import-overview-and-dedup](08-import-overview-and-dedup.tech.md).

## BBVA (`BbvaCsvImportService`)
- Parser custom, delimitatore `;` (gestisce campi quotati).
- Header individuato cercando un set fisso di colonne richieste: `Data valuta`, `Data`, `Parola chiave`, `Movimento`, `Importo`, `Disponibile`, `Osservazioni`.
- Date `dd/MM/yyyy` italiane, importi con virgola decimale, strip di `€`/`EUR`.
- `ExternalId` = fingerprint SHA-256 di data+importo+saldo disponibile+campi testo normalizzati, prefisso `"BBVA:v3:"`.
- Nessun transfer matching.

## Sella (`SellaCsvImportService`)
- `CsvHelper` con row model `[Name]`-attributed: `Codice identificativo`, `Data operazione`, `Data valuta`, `Descrizione`, `Importo`.
- Salta le righe "Saldo al ...".
- Estrae l'IBAN controparte dalla descrizione via regex.
- `ExternalId` = `Codice identificativo`.

## Satispay (`SatisPayCsvImportService`)
- `CsvHelper`, delimitatore `;`.
- Rimuove la sezione "Legenda" finale prima del parsing.
- Scarta le righe con `Stato` = "Annullato".
- Estrae l'IBAN dal testo tra parentesi nella descrizione (es. `(IT21W...)`).
- `ExternalId` = `ID`. Dedup applicata anche con `importAll=true` (l'indice univoco non è comunque bypassabile).

## Trade Republic (`TradeRepublicCsvImportService`)
- `CsvHelper`, colonne inglesi: `datetime`, `account_type`, `type`, `name`, `amount`, `fee`, `tax`, `description`, `transaction_id`, `counterparty_iban`.
- Importo effettivo = `amount` + `fee` + `tax`.
- `account_type == "TRADING"` può instradare su un account separato "Trade Republic Trading".
- Force-categorizzazione a `SavingsAndInvestments` per tipi `BUY`/`SELL`/`DIVIDEND`/`INTEREST_PAYMENT`/`BENEFITS_SAVEBACK`.
- `ExternalId` = `transaction_id`.

## Endpoint `*-xlsx`
`ImportController` converte il body `.xlsx` in CSV in-memory (`ConvertExcelToCsv` via `ExcelDataReader`) e lo instrada allo stesso importer CSV — permette di riusare parsing/dedup/matching quando la banca esporta solo in Excel nativo.
