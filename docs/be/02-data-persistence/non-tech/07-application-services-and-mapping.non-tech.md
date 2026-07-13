# 07 - La logica applicativa (Non Tecnico)

## Cosa orchestra
Al di sopra del semplice salvataggio, il backend applica le regole di business: quale categoria assegnare, come costruire un trasferimento a partire da due movimenti, come calcolare saldi e riepiloghi.

## Esempi concreti
- Quando si elimina una categoria, i movimenti collegati non vengono persi ma spostati automaticamente su una categoria "Generico".
- Un trasferimento tra due conti viene sempre registrato come coppia coerente di movimenti, mai come un movimento isolato.
- Ogni conto è sempre associato al proprietario corretto: anche se un dato arrivasse manomesso dal client, il backend impone comunque il proprietario reale, autenticato.
- Per l'account dimostrativo, il backend sa costruire da solo una storia finanziaria plausibile di diversi mesi (stipendio, affitto, spese quotidiane, risparmi), cancellando prima ogni dato demo precedente.

## Valore
L'utente non deve preoccuparsi della coerenza dei dati: il backend la garantisce automaticamente a ogni operazione, comprese le regole di proprietà e visibilità tra utenti diversi.
