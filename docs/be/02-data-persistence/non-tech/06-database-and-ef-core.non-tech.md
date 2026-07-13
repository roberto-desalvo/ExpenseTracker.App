# 06 - Come sono salvati i dati (Non Tecnico)

## Dove vivono i dati
Tutti gli utenti, i conti, le categorie, i movimenti e i trasferimenti sono conservati in un database centrale, sempre aggiornato in modo automatico quando la struttura dei dati evolve.

## Come sono nati gli utenti
La tabella che identifica gli utenti è stata introdotta con un aggiornamento pensato per non perdere nulla dei dati già presenti: i conti creati prima di questa modifica restano intatti e vengono ricollegati al proprietario corretto con un passaggio successivo, senza interruzioni per chi usa già l'applicazione.

## Come si evita di perdere o duplicare informazioni
Ogni movimento importato porta con sé un'impronta univoca: se lo stesso movimento viene importato due volte, il sistema lo riconosce e lo scarta automaticamente.

## Valore
I dati restano affidabili nel tempo, anche importando più volte lo stesso estratto conto o aggiornando la struttura dell'applicazione.
