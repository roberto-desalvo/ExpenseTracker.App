# 11 - Dashboard e trend (Non Tecnico)

## Scopo
Offrire una sintesi immediata dell'andamento economico e, quando serve, un'analisi più approfondita nel tempo, senza dover consultare fogli esterni.

## Dashboard
La pagina iniziale risponde in pochi secondi a "come sto andando questo mese?":
- **Patrimonio netto**: il totale di tutti i conti, quanto è cambiato negli ultimi 12 mesi e una piccola linea di andamento. Con l'icona a forma di occhio si nascondono gli importi (utile in pubblico); la scelta viene ricordata.
- **Quattro indicatori del mese**: entrate, uscite (entrambe confrontate con la media degli ultimi 6 mesi, in verde se il confronto è favorevole, in rosso se no), risparmio netto con l'avanzamento del mese e tasso di risparmio (quanta parte delle entrate resta dopo le spese).
- **Flusso di cassa**: per ciascuno degli ultimi 12 mesi, le entrate come barra verso l'alto, le uscite verso il basso e il saldo come linea; passandoci sopra si vede anche il tasso di risparmio del mese. I trasferimenti tra i propri conti non sono contati.
- **Dove vanno i soldi**: le categorie con più spese (o entrate) del mese, ordinate dalla più grande, con importo e percentuale.
- **Conti**: come il patrimonio è ripartito tra i conti e quanto si è mosso ciascun conto nel mese.
- **Ultimi movimenti**: gli ultimi sette movimenti; cliccando su uno lo si modifica.

## Analisi
La pagina Analisi permette di scegliere un periodo (3, 6, 12, 24 mesi, da inizio anno o date a piacere) e come raggruppare i dati (giorno, settimana, mese, anno). Mostra:
- in evidenza, il flusso di cassa per periodo con le cifre chiave del periodo (patrimonio iniziale e finale, variazione, entrate e uscite totali, tasso di risparmio); passando sopra un periodo si vedono entrate, uscite, saldo e tasso di risparmio di quel periodo;
- l'andamento del patrimonio;
- il saldo di ogni conto nel tempo (cliccando sul nome di un conto nella legenda lo si nasconde);
- come si compongono spese o entrate per categoria in ogni periodo, e la classifica delle categorie sul periodo intero.

Ogni grafico ha anche una **vista a tabella** con i numeri esatti.

Per i grafici del patrimonio e dei saldi, il punto di ogni periodo è la giacenza *media* di quel periodo, non il saldo puntuale a fine periodo; nei periodi senza movimenti il saldo resta quello precedente. Questi saldi includono i giroconti (un giroconto sposta davvero denaro dentro o fuori da un conto), mentre i grafici di entrate e uscite li escludono.

## Come sono pensati i grafici
- Un solo asse in euro per grafico, così le grandezze sono sempre confrontabili.
- Ogni conto ha sempre lo stesso colore in tutta l'app, anche quando se ne nascondono alcuni.
- Le categorie minori vengono raggruppate in "Altre" per non affollare i grafici di colori.
- I colori sono scelti per restare distinguibili anche da chi ha difficoltà a percepire i colori, e ogni grafico ha legenda e valori al passaggio del mouse.

## Valore
Aiuta a prendere decisioni rapide sul mese in corso e a capire le tendenze su un arco di tempo più ampio.
