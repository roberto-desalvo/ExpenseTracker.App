# 12 - Pubblicazione e configurazione (Non Tecnico)

## Come arriva in produzione
Ogni modifica confermata sul ramo principale viene automaticamente compilata e pubblicata online, senza intervento manuale. Il backend viene pubblicato su un servizio cloud serverless (Azure Functions), che si accende solo quando riceve richieste.

## Cosa serve configurare una volta
Il nome del servizio cloud di destinazione e, facoltativamente, l'indirizzo del frontend autorizzato a dialogare con il backend sono impostati nelle impostazioni del repository: la pubblicazione li legge automaticamente.

## Cosa manca ancora
Il rilascio automatico non include, al momento, un controllo automatico che i test passino prima della pubblicazione: è un miglioramento di processo da valutare.

## Come viene configurato l'ambiente
Le impostazioni sensibili (accessi, connessioni al database) sono gestite separatamente dal codice, cambiano tra ambiente di sviluppo e produzione, e non vengono mai pubblicate insieme al codice sorgente. In locale ogni sviluppatore parte da un modello di configurazione condiviso e inserisce i propri valori in un file personale non salvato nel repository.
