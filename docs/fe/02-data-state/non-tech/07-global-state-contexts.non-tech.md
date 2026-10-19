# 07 - Stato condiviso (Non Tecnico)

## Concetto
L'app mantiene informazioni comuni (filtri, liste, selezioni, preferenze) in un unico livello condiviso.

## Cosa viene condiviso
- Elenchi di conti, categorie e movimenti, con paginazione e ricarica automatica dopo ogni modifica.
- I filtri della pagina Movimenti: mese selezionato, tipo di movimento (tutti/entrate/uscite), conti e categorie selezionati.
- Le finestre per creare o modificare un movimento e per registrare un trasferimento, apribili da qualsiasi pagina.
- I messaggi di conferma ("operazione riuscita") e di errore, mostrati come notifiche in basso a destra.
- Le preferenze di aspetto: stile, tema chiaro/scuro/automatico, posizione del menu.

Quando si salva, modifica o elimina un movimento, anche la Dashboard, l'Analisi e i saldi dei Conti si aggiornano da soli.

Alcune informazioni vengono caricate solo quando servono (ad esempio l'elenco dei movimenti solo nella pagina Movimenti), per evitare chiamate inutili.

## Benefici
- Coerenza tra sezioni diverse.
- Aggiornamenti più rapidi dopo un salvataggio.
- Riduzione di duplicazioni di logica.
- Notifiche uniformi di successo ed errore in tutta l'app.
