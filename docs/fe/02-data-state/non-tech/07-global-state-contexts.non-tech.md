# 07 - Stato condiviso (Non Tecnico)

## Concetto
L'app mantiene informazioni comuni (filtri, liste, selezioni) in un unico livello condiviso.

## Cosa viene condiviso
- Elenchi di conti, categorie e movimenti, con paginazione e ricarica automatica dopo ogni modifica.
- I filtri della tabella movimenti: mese selezionato, tipo di movimento (tutti/entrate/uscite), conti e categorie selezionati.
- Il form per creare o modificare un movimento, mostrato come finestra sovrapposta (modale).
- I messaggi di conferma ("operazione riuscita") e di errore, mostrati come notifiche in basso a destra.
- Il tema grafico scelto dall'utente (chiaro o scuro).

Alcune di queste informazioni vengono caricate solo quando servono davvero (ad esempio conti e categorie si aggiornano solo nella home e nelle impostazioni), per evitare chiamate inutili.

## Benefici
- Coerenza tra sezioni diverse.
- Aggiornamenti più rapidi dopo un salvataggio.
- Riduzione di duplicazioni di logica.
- Notifiche uniformi di successo ed errore in tutta l'app.
