# 03 - Le funzionalità esposte (Non Tecnico)

## Cosa può fare chi usa l'app
- Consultare e cercare conti, categorie, movimenti e trasferimenti.
- Creare, modificare ed eliminare ciascuno di questi elementi.
- Vedere un riepilogo generale (dashboard) con saldi, categorie e andamento nel tempo.
- Caricare estratti conto bancari per importare automaticamente i movimenti.
- Per l'account dimostrativo: rigenerare con un solo clic un set completo di dati d'esempio realistici (conti, movimenti, trasferimenti), cancellando quelli precedenti.

## Chi può farlo
Solo utenti autenticati, e ciascuno vede e modifica soltanto i propri conti, movimenti e trasferimenti. L'importazione dei file richiede inoltre un permesso specifico dedicato, non assegnato a tutti gli utenti. La rigenerazione dei dati demo è riservata al solo account contrassegnato come dimostrativo: chiunque altro provi a richiederla riceve un rifiuto.

## Dove girano queste funzionalità
Ogni operazione è una piccola "funzione" ospitata su un servizio cloud serverless: si attiva quando arriva una richiesta e scala automaticamente con il carico, senza un server sempre acceso. Gli indirizzi usati dal frontend e dagli script di importazione sono rimasti gli stessi.

## Provare le operazioni a mano
Per chi sviluppa o verifica il sistema c'è una raccolta pronta da importare in Postman (uno strumento per inviare richieste a un servizio web) con tutte le operazioni disponibili e dati di esempio, insieme a una configurazione già pronta per il servizio avviato in locale. Basta indicare l'indirizzo del servizio e un accesso valido per provarle una per una.

## Valore
Un unico punto di accesso, coerente e protetto, per tutte le operazioni sui dati economici, con costi proporzionati all'uso effettivo.
