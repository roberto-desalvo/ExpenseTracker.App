# 06 - Come sono salvati i dati (Non Tecnico)

## Dove vivono i dati
Tutti gli utenti, i conti, le categorie, i movimenti e i trasferimenti sono conservati in un database centrale, sempre aggiornato in modo automatico quando la struttura dei dati evolve.

## Come sono nati gli utenti
La tabella che identifica gli utenti è stata introdotta con un aggiornamento pensato per non perdere nulla dei dati già presenti: i conti creati prima di questa modifica restano intatti e vengono ricollegati al proprietario corretto con un passaggio successivo, senza interruzioni per chi usa già l'applicazione.

Un utente può essere ritrovato anche tramite la sua email: è il modo in cui l'accesso di sviluppo si aggancia a un profilo già esistente. Se più profili condividono la stessa email, viene scelto quello collegato all'app di import automatico, altrimenti il più vecchio.

## Come restano separati i dati degli utenti
Ogni movimento appartiene al proprietario del conto su cui è registrato: quando il sistema legge movimenti, riepiloghi o trasferimenti, considera solo quelli dei conti dell'utente collegato.

## Come si evita di perdere o duplicare informazioni
Ogni movimento importato porta con sé un'impronta univoca: se lo stesso utente importa due volte lo stesso movimento, il sistema lo riconosce e lo scarta automaticamente. Il controllo vale per singolo utente: due persone diverse possono importare lo stesso movimento (es. un conto cointestato) senza che il secondo venga scartato.

## Valore
I dati restano affidabili nel tempo, anche importando più volte lo stesso estratto conto o aggiornando la struttura dell'applicazione.
