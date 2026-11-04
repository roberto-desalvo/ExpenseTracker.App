# 12 - Pubblicazione e configurazione (Non Tecnico)

## Come arriva in produzione
Ogni modifica confermata sul ramo principale viene automaticamente compilata e pubblicata online, senza intervento manuale. Il backend viene pubblicato su un servizio cloud serverless (Azure Functions), che si accende solo quando riceve richieste.

Prima di pubblicare il nuovo backend, la stessa procedura aggiorna automaticamente la struttura del database (le "migrazioni"), così il codice appena rilasciato trova sempre i dati nella forma che si aspetta. Vengono applicati solo gli aggiornamenti non ancora presenti, uno alla volta e mai da due rilasci contemporaneamente; l'aggiornamento si può lanciare anche a mano, per esempio dopo aver ripristinato un backup. Per farlo la procedura apre per pochi istanti l'accesso al database solo dal computer che esegue il rilascio e lo richiude subito dopo, anche in caso di errore.

## Dove gira
I servizi cloud su cui viene pubblicato il backend sono a loro volta creati e aggiornati automaticamente a partire da file nel repository: vedi il modulo 13 (Infrastructure as Code).

## Cosa serve configurare una volta
Per l'aggiornamento automatico del database, una sola volta: autorizzare l'identità di pubblicazione a modificare la struttura del database (lo stesso script che autorizza il backend a leggere e scrivere i dati) e a gestire le regole di accesso del server database (preparazione iniziale, modulo 13). Per il resto niente di specifico: la pubblicazione usa la stessa identità senza password della procedura che crea l'infrastruttura (modulo 13) e ricava da sola su quale servizio pubblicare, leggendolo dall'ultima creazione dell'infrastruttura. Non ci sono chiavi di pubblicazione da copiare o rinnovare, e l'accesso con credenziali semplici al servizio resta spento. Quali siti sono autorizzati a dialogare con il backend non si imposta più qui: è definito insieme al resto dell'infrastruttura (modulo 13).

## Cosa manca ancora
Il rilascio automatico non include, al momento, un controllo automatico che i test passino prima della pubblicazione: è un miglioramento di processo da valutare.

## Come viene configurato l'ambiente
Le impostazioni sensibili (accessi, connessioni al database) sono gestite separatamente dal codice, cambiano tra ambiente di sviluppo e produzione, e non vengono mai pubblicate insieme al codice sorgente. In locale ogni sviluppatore parte da un modello di configurazione condiviso e inserisce i propri valori in un file personale non salvato nel repository. Il modello locale attiva già l'accesso semplificato di sviluppo (solo nome utente), così non serve configurare l'identità Microsoft per provare l'app sul proprio computer. La demo pubblica, invece, è spenta finché non viene attivata esplicitamente sul servizio online insieme a una chiave segreta dedicata; se la chiave manca o è troppo debole, il servizio si rifiuta di partire. Anche l'import automatico da OneDrive è spento di default, sia in locale sia online: si accende dalla configurazione, dopo aver concesso una volta l'accesso al proprio OneDrive.

Gli IBAN dei conti non fanno parte della configurazione del servizio: sono salvati insieme ai conti nel database e si gestiscono dalla pagina dei conti, così non vanno persi se il servizio cloud viene ricreato o spostato.
