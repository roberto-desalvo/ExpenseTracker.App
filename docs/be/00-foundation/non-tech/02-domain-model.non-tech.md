# 02 - Il modello dei dati (Non Tecnico)

## I concetti principali
- **Utente**: la persona che usa l'applicazione, identificata tramite il proprio accesso Microsoft/Azure AD.
- **Conto**: dove si trovano i soldi (es. contanti, carta, banca), sempre di proprietà di un utente.
- **Categoria**: il tipo di spesa o entrata (es. spesa alimentare, stipendio).
- **Movimento**: un singolo ingresso o uscita di denaro.
- **Trasferimento**: uno spostamento di denaro tra due conti dell'utente, non un guadagno né una spesa reale.

## Come si collegano
Ogni conto appartiene a un utente. Ogni movimento appartiene a un conto ed è quasi sempre associato a una categoria. Un trasferimento è composto da due movimenti collegati (uno in uscita da un conto, uno in entrata sull'altro).

## Come nasce un conto
Non esiste più un elenco fisso di conti predefiniti: al primo caricamento di un estratto conto, se il conto della banca non esiste ancora per quell'utente, viene creato automaticamente.

## Perché è importante
Questa struttura permette di distinguere in modo affidabile le spese reali dai semplici spostamenti di denaro tra i propri conti, evitando di falsare i totali, e garantisce che i dati economici di ciascun utente restino separati da quelli degli altri.
