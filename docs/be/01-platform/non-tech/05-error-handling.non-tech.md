# 05 - Gestione degli errori (Non Tecnico)

## Cosa succede quando qualcosa va storto
Il backend non si blocca mai in modo silenzioso: ogni errore (dato mancante, richiesta non valida, elemento non trovato, ecc.) viene riconosciuto e trasformato in un messaggio chiaro.

## Tipi di errore riconosciuti
- Richiesta non corretta o incompleta.
- Dati non validi.
- Utente non autorizzato.
- Elemento non trovato.
- Conflitto (es. tentativo di eliminare qualcosa ancora in uso).

## Valore
Il frontend può sempre mostrare all'utente un messaggio d'errore appropriato invece di un comportamento imprevedibile.
