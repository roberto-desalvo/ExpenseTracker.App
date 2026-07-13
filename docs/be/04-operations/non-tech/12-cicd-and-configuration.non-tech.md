# 12 - Pubblicazione e configurazione (Non Tecnico)

## Come arriva in produzione
Ogni modifica confermata sul ramo principale viene automaticamente compilata e pubblicata online, senza intervento manuale.

## Cosa manca ancora
Il rilascio automatico non include, al momento, un controllo automatico che i test passino prima della pubblicazione: è un miglioramento di processo da valutare.

## Come viene configurato l'ambiente
Le impostazioni sensibili (accessi, connessioni al database) sono gestite separatamente dal codice, cambiano tra ambiente di sviluppo e produzione, e non vengono mai pubblicate insieme al codice sorgente.
