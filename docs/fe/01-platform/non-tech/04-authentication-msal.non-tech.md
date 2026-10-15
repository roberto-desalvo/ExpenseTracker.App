# 04 - Accesso utente e sicurezza (Non Tecnico)

## Come funziona
L'accesso avviene tramite account aziendale Microsoft (Azure AD). Dopo il primo accesso, l'app rinnova automaticamente il permesso di dialogare col backend senza richiedere ulteriori interazioni, finché la sessione del browser resta valida.

## Risultato
- Solo utenti autorizzati vedono i dati.
- Le sessioni sono protette e legate alla scheda del browser in uso.

## Se qualcosa va storto
L'utente riceve una schermata d'errore con opzione di nuovo tentativo.

## In sviluppo
Quando l'app viene avviata in locale dallo sviluppatore, l'accesso Microsoft è saltato: viene chiesto solo il nome utente con cui entrare, ed è possibile cambiare utente uscendo e indicando un altro nome. Questa modalità esiste solo in sviluppo e non è mai presente nella versione pubblicata.
