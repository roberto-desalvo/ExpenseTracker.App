# 10 - Riconoscimento dei trasferimenti (Non Tecnico)

## Il problema che risolve
Quando si sposta denaro tra due conti propri (es. da Trade Republic a Satispay), la banca di origine registra un'uscita e la banca di destinazione registra un'entrata, come due operazioni separate e apparentemente scollegate.

## Come viene risolto
L'applicazione riconosce automaticamente queste coppie confrontando importo, data e descrizione, anche se provengono da import di banche diverse fatti in momenti diversi, e le unisce in un unico "trasferimento".

Per riconoscere i bonifici tra conti propri usa l'IBAN: ogni conto può avere il suo IBAN, che si inserisce nella pagina dei conti. Quando un movimento importato riporta l'IBAN di un altro conto dell'utente, viene registrato subito come trasferimento.

## Cosa succede se non trova subito la coppia
Il movimento viene comunque salvato normalmente; verrà collegato automaticamente non appena verrà importato anche il movimento della banca controparte.

## Sistemare i trasferimenti già importati
Se in passato degli import sono avvenuti senza riconoscere i trasferimenti (per esempio prima di aver inserito gli IBAN dei conti), è possibile avviare una "riconciliazione": l'applicazione rilegge i movimenti già presenti di tutti gli utenti, cerca le coppie entrata/uscita tra conti diversi della stessa persona con lo stesso importo e date vicine (al massimo tre giorni di differenza) e le collega come trasferimenti. Non serve indicare per chi farlo: ogni persona viene analizzata separatamente e un movimento non viene mai abbinato a quello di un altro utente. L'operazione è pensata per grandi volumi: anche decine di migliaia di movimenti vengono analizzati in pochi secondi.

- Si può prima fare una prova, che mostra le coppie trovate senza modificare nulla.
- Si può limitare la ricerca a un periodo.
- Quando un movimento ha più controparti ugualmente plausibili, viene lasciato com'è invece di indovinare.

## Isolamento tra utenti
La ricerca della controparte avviene solo tra i conti della stessa persona che ha effettuato l'import: un movimento non può mai essere abbinato per errore a un conto di un altro utente dell'applicazione.

## Valore
I trasferimenti tra i propri conti non vengono mai conteggiati per errore come spese o entrate reali, e restano sempre correttamente separati tra le diverse persone che usano l'applicazione. Gli errori di import passati si possono correggere senza rifare l'import.
