# 04 - Accesso utente e sicurezza (Non Tecnico)

## Come funziona
L'accesso avviene tramite account aziendale Microsoft (Azure AD). Dopo il primo accesso, l'app rinnova automaticamente il permesso di dialogare col backend senza richiedere ulteriori interazioni, finché la sessione del browser resta valida.

## Prima schermata: accesso o demo
All'apertura dell'app, se non si è già entrati, compare una schermata con due scelte: accedere con l'account Microsoft (come sempre) oppure "Prova la demo". La demo fa entrare subito, senza credenziali, come utente di test con dati fittizi; l'accesso si rinnova da solo finché la scheda resta aperta, e "Esci" riporta alla schermata iniziale. Se la demo non è disponibile compare un messaggio, e l'accesso Microsoft resta utilizzabile. Il pulsante della demo può essere nascosto dalla configurazione.

## Risultato
- Solo utenti autorizzati vedono i dati.
- Le sessioni sono protette e legate alla scheda del browser in uso.

## Se qualcosa va storto
L'utente riceve una schermata d'errore con opzione di nuovo tentativo.

## In sviluppo
Quando l'app viene avviata in locale dallo sviluppatore, l'accesso Microsoft è saltato: viene chiesto solo il nome utente con cui entrare, ed è possibile cambiare utente uscendo e indicando un altro nome. Questa modalità esiste solo in sviluppo e non è mai presente nella versione pubblicata.
