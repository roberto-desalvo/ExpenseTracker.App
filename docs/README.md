# Documentazione ExpenseTracker

ExpenseTracker è una web app personale per tenere traccia di entrate, uscite e trasferimenti tra conti. I movimenti arrivano soprattutto dagli estratti conto delle banche (caricati a mano o salvati in una cartella OneDrive), vengono deduplicati, categorizzati e collegati quando formano un giroconto; dashboard e analisi mostrano saldi e andamenti nel tempo.

## Documenti

| # | Documento | Contenuto |
|---|---|---|
| 1 | [Architettura](01-architettura.md) | Scelte tecniche principali e componenti del sistema |
| 2 | [Backend](02-backend.md) | Layer della solution, pipeline delle richieste, endpoint, import |
| 3 | [Frontend](03-frontend.md) | Organizzazione dell'interfaccia, pagine, stato, temi |
| 4 | [Dati](04-dati.md) | Schema del database, vincoli e regole sui dati |
| 5 | [Operazioni](05-operazioni.md) | Infrastruttura, CI/CD, configurazione, test |

## Funzionalità in sintesi

- Dashboard con patrimonio, entrate/uscite del mese, conti, categorie e ultimi movimenti.
- Analisi nel tempo: patrimonio, flusso di cassa, spese per categoria, classifiche.
- Gestione di movimenti, trasferimenti, categorie (con parole chiave per la categorizzazione automatica) e conti (con IBAN).
- Import di CSV/XLSX di BBVA, Sella, Satispay e Trade Republic, più un formato Excel storico.
- Import automatico dalle cartelle `<Banca>/` della cartella dell'app nel OneDrive personale che ogni utente collega (l'app non vede il resto del drive).
- Riconoscimento automatico dei trasferimenti tra i propri conti.
- Modalità demo con dati fittizi, rigenerati ogni notte.

## Manutenzione

Questi documenti descrivono le scelte e la struttura, non ogni dettaglio: per quello la fonte è il codice. Quando una modifica cambia una scelta tecnica, un componente, un endpoint, una pagina, lo schema dei dati o la configurazione, aggiorna il documento corrispondente. Non serve riportare la storia delle modifiche (c'è git).
