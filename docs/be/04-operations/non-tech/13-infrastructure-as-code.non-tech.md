# 13 - Infrastructure as Code (Non Tecnico)

## Cos'è
I servizi cloud su cui gira l'applicazione (il backend, il sito del frontend, l'archivio dei segreti e il monitoraggio) sono descritti in file di testo salvati nel repository, invece di essere creati a mano dal portale di Azure. In questo modo l'ambiente è documentato, ripetibile e ogni modifica è tracciata come una modifica al codice.

Il database fa eccezione: è un servizio già esistente, che la procedura non crea né modifica. Si limita a sapere come si chiama (l'informazione viene fornita nelle impostazioni della procedura) per collegarvi il backend, così i dati esistenti restano al sicuro.

## Come funziona il rilascio
Quando una modifica a questi file viene proposta, una procedura automatica mostra in anticipo cosa cambierebbe su Azure (cosa verrebbe creato, modificato o lasciato invariato), così da poterla valutare prima di approvarla. Quando la modifica entra nel ramo principale, la stessa procedura la applica davvero all'ambiente di produzione, che può essere protetto da un'approvazione manuale.

## Rilascio dal proprio computer
Le stesse impostazioni usate dalla procedura automatica si possono tenere in un file personale sul proprio computer, escluso dal repository perché può contenere informazioni riservate. Un piccolo script le carica prima del rilascio e avvisa se ne manca qualcuna indispensabile. Nel repository resta solo un modello vuoto da copiare e compilare.

## Scelte di sicurezza e costo
- Il backend accede al database e ai propri archivi con un'identità gestita da Azure, senza password salvate nella configurazione.
- La chiave segreta della demo pubblica è custodita in un archivio protetto dedicato e non nelle impostazioni del servizio.
- Il backend si accende solo quando riceve richieste, per contenere i costi; il monitoraggio ha un tetto giornaliero ai dati raccolti.

## Cosa resta da fare a mano
Alcune operazioni si fanno una sola volta, dopo la prima creazione dell'ambiente: autorizzare il backend a leggere e scrivere nel database esistente e aggiornare le procedure di pubblicazione del backend e del frontend con i nuovi riferimenti ai servizi creati.
