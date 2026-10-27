# 11 - Collaudo e qualità (Non Tecnico)

## Cosa viene verificato prima di ogni rilascio
Il funzionamento degli importer bancari viene testato in modo automatico: lettura corretta dei file, riconoscimento dei duplicati, riconoscimento dei trasferimenti tra conti (anche tramite l'IBAN dei conti), correttezza dei dati annotati nel registro degli import, e che un problema del registro non blocchi mai un caricamento. È verificata anche la riconciliazione dei trasferimenti già importati: quali coppie vengono collegate, quali vengono lasciate stare perché ambigue, che i movimenti di persone diverse non vengano mai abbinati, che la modalità di prova non modifichi nulla e che decine di migliaia di movimenti vengano elaborati in tempi brevi. Infine è verificata la riclassificazione dei movimenti tramite parole chiave: che vinca la categoria con priorità più alta, che i trasferimenti e i movimenti fuori dal periodo scelto restino invariati e che i movimenti senza corrispondenze mantengano la loro categoria, usando solo le categorie dell'utente. Per l'account dimostrativo si verifica che la rigenerazione dei dati riporti anche le categorie all'elenco di partenza senza toccare quelle degli altri utenti.

## Cosa manca ancora
Non sono ancora presenti verifiche automatiche sul rispetto della struttura architetturale interna del codice: è un miglioramento previsto ma non ancora realizzato.

## Valore
Ogni modifica agli importer bancari può essere verificata rapidamente prima di essere rilasciata, riducendo il rischio di importazioni errate.
