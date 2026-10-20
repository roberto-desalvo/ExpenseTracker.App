# 07 - La logica applicativa (Non Tecnico)

## Cosa orchestra
Al di sopra del semplice salvataggio, il backend applica le regole di business: quale categoria assegnare, come costruire un trasferimento a partire da due movimenti, come calcolare saldi e riepiloghi.

## Esempi concreti
- Quando si elimina una categoria, i movimenti collegati non vengono persi ma spostati automaticamente su una categoria "Generico".
- Un trasferimento tra due conti viene sempre registrato come coppia coerente di movimenti, mai come un movimento isolato.
- Ogni conto è sempre associato al proprietario corretto: anche se un dato arrivasse manomesso dal client, il backend impone comunque il proprietario reale, autenticato.
- L'IBAN di un conto viene ripulito da spazi e scritto in maiuscolo; se non ha un formato valido, o se la stessa persona l'ha già assegnato a un altro conto, il salvataggio viene rifiutato con un messaggio chiaro.
- Su richiesta, il backend ripercorre i movimenti già importati di tutti gli utenti e collega come trasferimenti le coppie entrata/uscita tra i conti di ciascuna persona; può anche limitarsi a mostrare cosa collegherebbe, senza modificare nulla.
- Ogni utente vede e gestisce solo i movimenti e i trasferimenti registrati sui propri conti: quelli degli altri utenti non compaiono nelle ricerche, nei riepiloghi o nei grafici, e non possono essere letti, modificati o eliminati nemmeno conoscendone l'identificativo. Anche creare un movimento o un trasferimento è possibile solo sui propri conti.
- Con l'accesso semplificato di sviluppo, il backend ritrova il profilo esistente a partire dall'email invece di crearne uno nuovo, così in locale si vedono i propri conti e movimenti reali.
- Per l'account dimostrativo, il backend sa costruire da solo una storia finanziaria plausibile di diversi mesi (stipendio, affitto, spese quotidiane, risparmi), cancellando prima ogni dato demo precedente.
- Per l'utente di test (`test@dev.local`) il backend ricrea da zero cinque conti (conto corrente, carta di credito, conto deposito, conto titoli e contanti) con circa cinque anni di movimenti credibili: stipendio con tredicesima, affitto e bollette che variano con le stagioni, spese quotidiane, vacanze estive e regali di Natale, saldo mensile della carta, prelievi e piccole spese in contanti, risparmio periodico e un piano di accumulo in titoli. I prezzi crescono leggermente anno dopo anno, e i saldi restano plausibili (il contante non va mai sotto zero). Se l'utente di test non esiste viene creato; i dati precedenti vengono sempre cancellati, così ogni esecuzione riparte da una situazione pulita.
- Nei grafici di andamento (es. "Account" e "Patrimonio" in home), il valore mostrato per ogni periodo (giorno/settimana/mese/anno) è la giacenza media di quel periodo, calcolata come media dei saldi rilevati a ogni movimento avvenuto in quel periodo; se un periodo non ha movimenti, si mostra il saldo invariato ereditato dal periodo precedente, così il grafico non presenta buchi.
- Il saldo di un conto include sempre anche i giroconti verso/da altri propri conti: un giroconto sposta comunque denaro dentro o fuori da quel conto specifico, anche se a livello di patrimonio complessivo si annulla. Per questo motivo i giroconti non vengono mai esclusi dal calcolo della giacenza, a differenza dei grafici di entrate/uscite dove invece è corretto escluderli (non sono guadagni o spese reali).
- Per ogni periodo delle serie temporali di entrate/uscite (usate ad esempio nel grafico "Categorie"), il backend calcola separatamente il totale entrate e il totale uscite di quel periodo, non solo il saldo netto: questo permette al frontend di mostrarli come due valori distinti (es. due barre affiancate) invece di un unico numero che nasconderebbe quanto si è guadagnato e quanto si è speso.

## Valore
L'utente non deve preoccuparsi della coerenza dei dati: il backend la garantisce automaticamente a ogni operazione, comprese le regole di proprietà e visibilità tra utenti diversi.
