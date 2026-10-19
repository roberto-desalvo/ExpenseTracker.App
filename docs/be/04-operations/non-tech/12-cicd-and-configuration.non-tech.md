# 12 - Pubblicazione e configurazione (Non Tecnico)

## Come arriva in produzione
Ogni modifica confermata sul ramo principale viene automaticamente compilata e pubblicata online, senza intervento manuale. Il backend viene pubblicato su un servizio cloud serverless (Azure Functions), che si accende solo quando riceve richieste.

## Cosa serve configurare una volta
La pubblicazione usa una "chiave di pubblicazione" fornita da Azure al momento della creazione del servizio cloud, salvata in modo protetto nelle impostazioni del repository. Perché la chiave funzioni, sul servizio Azure deve restare attivo l'accesso di pubblicazione con credenziali semplici; se la chiave viene rigenerata va aggiornata anche nel repository. Il nome del servizio di destinazione ha già un valore predefinito ma può essere cambiato; facoltativamente si può indicare anche l'indirizzo del frontend autorizzato a dialogare con il backend: la pubblicazione legge automaticamente queste impostazioni.

## Cosa manca ancora
Il rilascio automatico non include, al momento, un controllo automatico che i test passino prima della pubblicazione: è un miglioramento di processo da valutare.

## Come viene configurato l'ambiente
Le impostazioni sensibili (accessi, connessioni al database) sono gestite separatamente dal codice, cambiano tra ambiente di sviluppo e produzione, e non vengono mai pubblicate insieme al codice sorgente. In locale ogni sviluppatore parte da un modello di configurazione condiviso e inserisce i propri valori in un file personale non salvato nel repository. Il modello locale attiva già l'accesso semplificato di sviluppo (solo nome utente), così non serve configurare l'identità Microsoft per provare l'app sul proprio computer. La demo pubblica, invece, è spenta finché non viene attivata esplicitamente sul servizio online insieme a una chiave segreta dedicata; se la chiave manca o è troppo debole, il servizio si rifiuta di partire.
