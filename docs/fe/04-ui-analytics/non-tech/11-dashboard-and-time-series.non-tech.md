# 11 - Dashboard e trend (Non Tecnico)

## Scopo
Offrire una sintesi immediata dell'andamento economico, senza dover consultare fogli esterni.

## Cosa mostra
La Home è organizzata in due blocchi, ciascuno apribile/richiudibile a piacere (la scelta viene ricordata anche se si ricarica la pagina):

- **Questo mese**: saldo totale (nascondibile con un click per motivi di privacy), entrate, uscite e bilancio del mese corrente, l'elenco delle transazioni del mese e la distribuzione di entrate/uscite suddivisa per account e per categoria (grafici a ciambella).
- **Patrimonio**: andamento del patrimonio complessivo nel tempo, con la possibilità di scegliere un intervallo di date e una granularità (giornaliera, settimanale, mensile o annuale), più due grafici di dettaglio: uno per account e uno per categoria.

Per i grafici di andamento (patrimonio totale e per account), il punto di ogni periodo rappresenta la giacenza *media* di quel periodo (non il saldo puntuale a fine periodo): se in un mese avvengono più movimenti, il valore mostrato è la media dei saldi rilevati a ogni movimento; nei mesi senza movimenti il saldo resta invariato rispetto al mese precedente, così la linea del grafico non si interrompe. Questi saldi includono sempre anche i giroconti tra i propri conti (un giroconto sposta comunque denaro dentro o fuori da un conto specifico), a differenza dei grafici di entrate/uscite dove i giroconti vengono correttamente esclusi. Entrambi i grafici sono disegnati come linee "a gradini": il valore resta piatto fino al periodo successivo invece di essere collegato con una curva, per rendere più evidente che il saldo cambia a scatti, non gradualmente.

Il grafico per categoria non è più una linea ma un istogramma: per ogni periodo e per ogni categoria mostra due barre affiancate, una verde con il totale delle entrate e una rossa con il totale delle uscite (sempre in valore assoluto), così si vede a colpo d'occhio quanto si è guadagnato e quanto si è speso in ciascuna categoria, periodo per periodo.

## Valore
Aiuta a prendere decisioni rapide sull'andamento economico personale, sia sul mese in corso sia su un arco di tempo più ampio, senza analisi manuali su fogli esterni.
