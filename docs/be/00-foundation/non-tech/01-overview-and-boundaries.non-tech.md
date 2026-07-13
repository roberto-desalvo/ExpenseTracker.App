# 01 - Visione e confini (Non Tecnico)

## A cosa serve il backend
Il backend è il motore che custodisce e gestisce tutti i dati economici dell'utente: conti, categorie, movimenti e trasferimenti.

## Cosa fa
- Salva in modo sicuro e coerente ogni movimento economico.
- Applica le regole di dominio: evita duplicati, assegna categorie, riconosce i trasferimenti tra conti.
- Espone queste informazioni al frontend tramite un canale protetto.

## Cosa non fa
- Non gestisce l'interfaccia grafica.
- Non decide cosa mostrare all'utente: fornisce solo i dati e le operazioni disponibili.

## Valore
Garantisce che i dati economici siano sempre corretti, protetti e coerenti, indipendentemente da quale banca o quale importazione li abbia originati.
