# 10 - Riconoscimento dei trasferimenti (Non Tecnico)

## Il problema che risolve
Quando si sposta denaro tra due conti propri (es. da Trade Republic a Satispay), la banca di origine registra un'uscita e la banca di destinazione registra un'entrata, come due operazioni separate e apparentemente scollegate.

## Come viene risolto
L'applicazione riconosce automaticamente queste coppie confrontando importo, data e descrizione, anche se provengono da import di banche diverse fatti in momenti diversi, e le unisce in un unico "trasferimento".

## Cosa succede se non trova subito la coppia
Il movimento viene comunque salvato normalmente; verrà collegato automaticamente non appena verrà importato anche il movimento della banca controparte.

## Valore
I trasferimenti tra i propri conti non vengono mai conteggiati per errore come spese o entrate reali.
