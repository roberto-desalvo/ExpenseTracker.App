# 04 - Accesso utente e sicurezza (Non Tecnico)

## Come si accede
L'accesso avviene tramite l'account Microsoft/Azure AD dell'utente: nessuna password è gestita direttamente dall'applicazione.

## Cosa succede al primo accesso
La prima volta che una persona accede, l'applicazione la riconosce tramite l'identità Microsoft/Azure AD e crea automaticamente il suo profilo interno, senza bisogno di una registrazione separata. Agli accessi successivi il profilo esistente viene semplicemente riconosciuto e riutilizzato.

## Cosa è protetto
Tutte le funzionalità richiedono che l'utente sia autenticato. Il caricamento di estratti conto è ulteriormente riservato a chi ha un permesso dedicato.

## Caricamento automatico da un'app collegata
Il caricamento degli estratti conto può avvenire anche in automatico, tramite un'app che opera per conto di un utente specifico invece che tramite una persona che carica manualmente un file dal browser. In questo caso l'app deve essere collegata in anticipo, una sola volta, all'utente per cui importa i dati; se non è ancora stata collegata, il caricamento automatico viene rifiutato.

## Cosa succede a ogni richiesta
Ogni richiesta viene verificata, tracciata e, in caso di problema, trasformata in un messaggio d'errore comprensibile invece di un blocco improvviso dell'applicazione.

## Valore
L'utente ha la garanzia che solo lui (o chi autorizzato) possa vedere e modificare i propri dati economici.
