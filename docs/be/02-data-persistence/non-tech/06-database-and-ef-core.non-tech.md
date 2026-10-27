# 06 - Come sono salvati i dati (Non Tecnico)

## Dove vivono i dati
Tutti gli utenti, i conti, le categorie, i movimenti e i trasferimenti sono conservati in un database centrale, sempre aggiornato in modo automatico quando la struttura dei dati evolve.

## Cosa succede se il database è "addormentato"
Il database usa un piano gratuito che si mette in pausa quando non viene usato per un po'. La prima richiesta dopo una pausa lo risveglia, e questo può richiedere fino a un minuto: invece di fallire subito, l'applicazione riprova automaticamente qualche volta, attendendo brevemente tra un tentativo e l'altro. La prima operazione dopo una pausa può quindi essere più lenta, ma va comunque a buon fine.

## Come sono nati gli utenti
La tabella che identifica gli utenti è stata introdotta con un aggiornamento pensato per non perdere nulla dei dati già presenti: i conti creati prima di questa modifica restano intatti e vengono ricollegati al proprietario corretto con un passaggio successivo, senza interruzioni per chi usa già l'applicazione.

Un utente può essere ritrovato anche tramite la sua email: è il modo in cui l'accesso di sviluppo si aggancia a un profilo già esistente. Se più profili condividono la stessa email, viene scelto quello collegato all'app di import automatico, altrimenti il più vecchio.

## Come restano separati i dati degli utenti
Ogni movimento appartiene al proprietario del conto su cui è registrato: quando il sistema legge movimenti, riepiloghi o trasferimenti, considera solo quelli dei conti dell'utente collegato.

Anche le categorie sono personali: ogni utente vede e modifica solo le proprie, e un movimento può essere assegnato solo a una categoria del suo stesso proprietario.

## Come sono diventate personali le categorie
Prima le categorie erano un elenco unico condiviso da tutti. Con il passaggio alle categorie per utente, l'elenco esistente (con le eventuali personalizzazioni) è stato assegnato al primo utente, il proprietario storico dei dati. Gli altri utenti già presenti, come l'account dimostrativo, hanno ricevuto l'elenco di partenza e i loro movimenti sono stati spostati sulle loro categorie equivalenti; quelli che erano in una categoria personalizzata del primo utente sono finiti nella loro categoria "Default".

L'elenco dei movimenti viene ordinato direttamente dal database secondo il criterio scelto (data, descrizione, categoria, conto o importo), così l'ordine resta coerente su tutte le pagine e non solo su quella visualizzata.

## L'IBAN dei conti
Ogni conto può avere un IBAN, facoltativo. Lo stesso IBAN non può essere assegnato a due conti della stessa persona, perché altrimenti non si saprebbe a quale conto attribuire un bonifico; persone diverse possono invece indicare lo stesso IBAN (es. un conto cointestato). I conti già esistenti restano senza IBAN finché non viene inserito.

## Come vengono trattati i giroconti nei riepiloghi
Nei conteggi di entrate e uscite (dashboard, analisi, totali dell'elenco movimenti) i trasferimenti tra i propri conti non vengono mai contati: spostare denaro da un conto all'altro non è né un guadagno né una spesa. Sono considerati giroconti sia le coppie di movimenti già collegate tra loro, sia i movimenti classificati nella categoria dei trasferimenti dell'utente per cui non è (ancora) stata trovata la controparte sull'altro conto. I saldi dei conti, invece, li includono sempre.

## Il registro degli import
Ogni caricamento di un estratto conto viene annotato in un registro dedicato (dettagli in [08-import-overview-and-dedup](../../03-import-pipeline/non-tech/08-import-overview-and-dedup.non-tech.md)). Le voci del registro appartengono all'utente che ha caricato il file. Se un conto viene eliminato le sue voci restano, ma senza il riferimento al conto.

## Come si evita di perdere o duplicare informazioni
Ogni movimento importato porta con sé un'impronta univoca: se lo stesso utente importa due volte lo stesso movimento, il sistema lo riconosce e lo scarta automaticamente. Il controllo vale per singolo utente: due persone diverse possono importare lo stesso movimento (es. un conto cointestato) senza che il secondo venga scartato.

## Valore
I dati restano affidabili nel tempo, anche importando più volte lo stesso estratto conto o aggiornando la struttura dell'applicazione.
