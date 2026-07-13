# 09 - Importazione per singola banca (Non Tecnico)

## Banche supportate
- BBVA
- Sella
- Satispay
- Trade Republic

## Perché servono importer diversi
Ogni banca esporta l'estratto conto con un proprio formato (colonne, simboli, lingua). Ogni importer sa "leggere" il formato specifico della propria banca e tradurlo negli stessi dati coerenti usati dall'applicazione.

Indipendentemente dalla banca, il conto a cui vengono agganciati i movimenti è sempre quello della persona che ha effettuato l'accesso, creato al volo se è il primo import di quella banca per quella persona.

## Comportamenti particolari
- Satispay: le operazioni annullate vengono ignorate.
- Trade Republic: gli investimenti (acquisti, vendite, dividendi) vengono categorizzati automaticamente come risparmio/investimento.

## Valore
Indipendentemente dalla banca di origine, l'utente ottiene sempre movimenti coerenti e confrontabili tra loro.
