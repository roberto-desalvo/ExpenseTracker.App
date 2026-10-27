# 08 - Importazione movimenti: panoramica (Non Tecnico)

## Cosa permette di fare
Caricare l'estratto conto scaricato dalla propria banca (CSV o Excel) e vedere i movimenti aggiunti automaticamente all'applicazione, senza inserimento manuale.

## Come evita i duplicati
Ogni movimento importato viene "marcato" in modo univoco. Se lo stesso file (o parte di esso) viene caricato più volte, i movimenti già presenti non vengono duplicati. Il controllo riguarda solo i movimenti di chi sta importando: se un'altra persona ha già importato lo stesso movimento (es. da un conto cointestato), viene comunque aggiunto anche per te.

## Cosa succede dopo il caricamento
Il sistema assegna automaticamente una categoria a ogni movimento quando possibile, e riconosce se un movimento fa parte di un trasferimento tra conti. Se in seguito si aggiungono o cambiano le parole chiave di una categoria, la stessa regola si può riapplicare ai movimenti già importati di un periodo, senza doverli ricaricare.

## Di chi sono i conti importati
Ogni file caricato viene sempre associato a chi ha effettuato l'accesso: se serve un conto nuovo (es. la prima volta che si importa l'estratto conto di una banca), viene creato automaticamente per quella persona, senza mescolare i dati tra utenti diversi.

## Il registro degli import
Ogni caricamento di un file lascia una traccia in un registro, sia quando va a buon fine sia quando fallisce. Per ogni caricamento vengono annotati:
- quando è stato fatto, da chi, con quale tipo di file e con quale nome;
- il conto a cui si riferisce l'estratto conto (per i file Excel che contengono più conti questo dato resta vuoto);
- il periodo coperto dal file, cioè la data del movimento più vecchio e di quello più recente, anche se alcuni erano già stati importati;
- quanti movimenti nuovi sono stati aggiunti e quanti trasferimenti tra conti sono stati creati;
- quanti movimenti sono stati scartati perché già presenti;
- l'eventuale messaggio di errore.

Così si può ricostruire quali periodi di ciascun conto sono già stati caricati e capire subito perché un caricamento non è andato a buon fine. Se il registro non riesce a salvare la traccia, il caricamento non viene comunque bloccato. Per ora il registro si consulta direttamente nel database: non c'è ancora una pagina dedicata nell'applicazione.

## Dati di test
Accanto agli import dei file c'è un'operazione speciale, riservata a chi ha lo stesso permesso di importazione, che non carica alcun file: cancella e ricrea i dati fittizi dell'utente di test, così un servizio automatico può riportarli periodicamente a uno stato pulito. Non tocca i dati di nessun altro utente.

## Valore
Import ripetuti e sicuri: si può ricaricare lo stesso estratto conto senza timore di sporcare i dati, e i movimenti di ciascuna persona restano sempre separati da quelli di chiunque altro usi l'applicazione. Il registro degli import rende ogni caricamento verificabile anche a distanza di tempo.
