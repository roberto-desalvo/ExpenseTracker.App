# 02 - Il modello dei dati (Non Tecnico)

## I concetti principali
- **Utente**: la persona che usa l'applicazione, identificata tramite il proprio accesso Microsoft/Azure AD.
- **Conto**: dove si trovano i soldi (es. contanti, carta, banca), sempre di proprietà di un utente. Può avere un IBAN, che serve a riconoscere i bonifici verso e da quel conto.
- **Categoria**: il tipo di spesa o entrata (es. spesa alimentare, stipendio).
- **Movimento**: un singolo ingresso o uscita di denaro.
- **Trasferimento**: uno spostamento di denaro tra due conti dell'utente, non un guadagno né una spesa reale.
- **Registro degli import**: la traccia di ogni caricamento di un estratto conto (quando, quale conto, che periodo, quanti movimenti aggiunti o scartati, eventuali errori).

## Come si collegano
Ogni conto appartiene a un utente. Ogni movimento appartiene a un conto ed è quasi sempre associato a una categoria. Un trasferimento è composto da due movimenti collegati (uno in uscita da un conto, uno in entrata sull'altro). Ogni voce del registro degli import appartiene a un utente e, quando il file riguarda un solo conto, indica anche quel conto; se il conto viene eliminato la voce resta, senza il riferimento al conto.

## Come nasce un conto
Non esiste più un elenco fisso di conti predefiniti: al primo caricamento di un estratto conto, se il conto della banca non esiste ancora per quell'utente, viene creato automaticamente.

## Perché è importante
Questa struttura permette di distinguere in modo affidabile le spese reali dai semplici spostamenti di denaro tra i propri conti, evitando di falsare i totali, e garantisce che i dati economici di ciascun utente restino separati da quelli degli altri.
