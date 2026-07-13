# 04 - Accesso utente e sicurezza (Non Tecnico)

## Come si accede
L'accesso avviene tramite l'account Microsoft/Azure AD dell'utente: nessuna password è gestita direttamente dall'applicazione.

## Cosa è protetto
Tutte le funzionalità richiedono che l'utente sia autenticato. Il caricamento di estratti conto è ulteriormente riservato a chi ha un permesso dedicato.

## Cosa succede a ogni richiesta
Ogni richiesta viene verificata, tracciata e, in caso di problema, trasformata in un messaggio d'errore comprensibile invece di un blocco improvviso dell'applicazione.

## Valore
L'utente ha la garanzia che solo lui (o chi autorizzato) possa vedere e modificare i propri dati economici.
