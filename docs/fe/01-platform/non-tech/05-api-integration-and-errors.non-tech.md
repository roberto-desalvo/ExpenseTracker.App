# 05 - Comunicazione con backend e gestione errori (Non Tecnico)

## Obiettivo
Scambiare dati con il backend in modo affidabile.

## Comportamento
- Ogni operazione (lettura/salvataggio) passa da un canale unico.
- Se il backend segnala un problema, l'utente vede un messaggio chiaro.
- Se la stessa lettura viene richiesta più volte nello stesso istante, al backend arriva una sola richiesta e la risposta viene condivisa.
- L'indirizzo del backend viene controllato in fase di pubblicazione: se è scritto in modo incompleto, la pubblicazione si ferma invece di mettere online un'app che non riesce a raggiungere i dati.

## Beneficio
Meno incoerenze nei messaggi e maggiore prevedibilità dell'esperienza.
