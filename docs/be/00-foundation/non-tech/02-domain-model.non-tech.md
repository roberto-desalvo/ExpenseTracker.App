# 02 - Il modello dei dati (Non Tecnico)

## I concetti principali
- **Conto**: dove si trovano i soldi (es. contanti, carta, banca).
- **Categoria**: il tipo di spesa o entrata (es. spesa alimentare, stipendio).
- **Movimento**: un singolo ingresso o uscita di denaro.
- **Trasferimento**: uno spostamento di denaro tra due conti dell'utente, non un guadagno né una spesa reale.

## Come si collegano
Ogni movimento appartiene a un conto ed è quasi sempre associato a una categoria. Un trasferimento è composto da due movimenti collegati (uno in uscita da un conto, uno in entrata sull'altro).

## Perché è importante
Questa struttura permette di distinguere in modo affidabile le spese reali dai semplici spostamenti di denaro tra i propri conti, evitando di falsare i totali.
