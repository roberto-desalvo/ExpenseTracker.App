# 13 - Infrastructure as Code (Non Tecnico)

## Cos'è
I servizi cloud su cui gira l'applicazione (il backend, il sito del frontend, l'archivio dei segreti e il monitoraggio) e le "registrazioni" che permettono agli utenti di accedere con il proprio account Microsoft sono descritti in file di testo salvati nel repository, invece di essere creati a mano dal portale di Azure. In questo modo l'ambiente è documentato, ripetibile e ogni modifica è tracciata come una modifica al codice.

Il database fa eccezione: è un servizio già esistente, che la procedura non crea né modifica. Si limita a sapere come si chiama (l'informazione viene fornita nelle impostazioni della procedura) per collegarvi il backend, così i dati esistenti restano al sicuro.

## Accesso degli utenti
La procedura crea due registrazioni nel sistema di identità Microsoft: una per il backend, che dichiara quali permessi esistono (ad esempio quello speciale per importare i file delle banche), e una per il sito, autorizzata in anticipo a usare il backend, così all'utente non viene chiesto alcun consenso aggiuntivo. Gli utenti sono riconosciuti tramite il loro account, non tramite la registrazione: rifare le registrazioni non fa perdere a nessuno i propri dati.

## Automatismi
L'ambiente comprende anche l'importazione automatica degli estratti conto salvati in una cartella OneDrive, con una sottocartella per ogni banca. Non è più un automatismo separato che controllava la cartella una volta al giorno: se ne occupa il backend stesso. OneDrive lo avvisa appena arriva un file, il backend lo importa entro pochi secondi e poi lo sposta tra quelli importati o tra quelli con errori. Il permesso di leggere il OneDrive va dato una volta a mano, accedendo con il proprio account Microsoft tramite uno script guidato: non si può fare in automatico. Da lì in poi il backend lo rinnova da solo. L'automatismo si accende con un'opzione della configurazione.

La rigenerazione giornaliera dei dati della demo pubblica invece non è più un servizio separato: la svolge il backend stesso a un orario fisso (di default le 3 di notte, ora UTC), modificabile dalle impostazioni della procedura. Il vecchio automatismo dedicato va cancellato a mano una volta, dopo il primo rilascio con questa modifica.

## Indirizzo del sito
Il sito può essere raggiunto anche da un indirizzo personalizzato (ad esempio expensetracker.lol). La procedura lo associa al sito e lo autorizza per l'accesso e per il dialogo con il backend; resta da aggiornare una volta la configurazione del dominio presso il registrar, che dimostra ad Azure di esserne il proprietario.

## Come funziona il rilascio
Quando una modifica a questi file viene proposta, una procedura automatica mostra in anticipo cosa cambierebbe su Azure (cosa verrebbe creato, modificato o lasciato invariato), così da poterla valutare prima di approvarla. Quando la modifica entra nel ramo principale, la stessa procedura la applica davvero all'ambiente di produzione. Le impostazioni e i segreti della procedura sono legati all'ambiente di produzione su GitHub, che può essere protetto da un'approvazione manuale.

## La preparazione iniziale
La procedura automatica ha bisogno di una propria identità con permessi precisi, e non può crearsela da sola. Per questo esiste una preparazione separata, anch'essa descritta in un file, che un amministratore esegue una volta dal proprio computer: crea il contenitore delle risorse dell'applicazione se non esiste ancora, crea l'identità della procedura, le concede solo i permessi strettamente necessari e assegna il permesso di importare i file agli utenti scelti e all'automatismo di importazione. Le operazioni più delicate restano così nelle mani di una persona e non della procedura automatica.

## Rilascio dal proprio computer
Le stesse impostazioni usate dalla procedura automatica si possono tenere in un file personale sul proprio computer, escluso dal repository perché può contenere informazioni riservate. Un piccolo script le carica prima del rilascio e avvisa se ne manca qualcuna indispensabile. Nel repository resta solo un modello vuoto da copiare e compilare.

## Scelte di sicurezza e costo
- Il backend accede al database e ai propri archivi con un'identità gestita da Azure, senza password salvate nella configurazione.
- La procedura automatica accede ad Azure senza password memorizzate e può modificare solo le risorse dell'applicazione.
- La chiave segreta della demo pubblica è custodita in un archivio protetto dedicato e non nelle impostazioni del servizio.
- Il backend si accende solo quando riceve richieste, per contenere i costi; il monitoraggio ha un tetto giornaliero ai dati raccolti.

## Cosa resta da fare a mano
Oltre alla preparazione iniziale, alcune operazioni si fanno una sola volta dopo la prima creazione dell'ambiente: eliminare i vecchi automatismi (rigenerazione della demo e import da OneDrive) che il backend ha sostituito, autorizzare l'accesso a OneDrive, configurare il dominio personalizzato, autorizzare il backend a leggere e scrivere nel database esistente (e la procedura di pubblicazione ad aggiornarne la struttura) e aggiornare le procedure di pubblicazione del backend e del frontend con i riferimenti ai servizi e alle registrazioni appena creati.
