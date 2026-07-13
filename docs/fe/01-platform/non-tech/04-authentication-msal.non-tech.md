# 04 - Accesso utente e sicurezza (Non Tecnico)

## Come funziona
L'accesso avviene tramite account aziendale Microsoft (Azure AD). Dopo il primo accesso, l'app rinnova automaticamente il permesso di dialogare col backend senza richiedere ulteriori interazioni, finché la sessione del browser resta valida.

## Risultato
- Solo utenti autorizzati vedono i dati.
- Le sessioni sono protette e legate alla scheda del browser in uso.

## Se qualcosa va storto
L'utente riceve una schermata d'errore con opzione di nuovo tentativo.
