# 09 - Flusso categorie (Non Tecnico)

## Obiettivo
Organizzare i movimenti per tipologia, così da migliorare lettura e controllo delle spese, e classificare in automatico i movimenti importati dai file della banca.

## Dove si trova
In **Impostazioni → Categorie**. L'andamento delle spese per categoria si trova invece nella Dashboard ("Dove vanno i soldi") e nella pagina Analisi.

Ogni utente vede e gestisce solo le proprie categorie, partendo da un elenco predefinito. Nell'account dimostrativo le categorie tornano all'elenco di partenza ogni volta che si rigenerano i dati demo.

## Azioni utente
- Cercare una categoria per nome.
- Creare o modificare una categoria in un pannello laterale, senza perdere di vista l'elenco: nome, descrizione, **parole chiave** e **priorità**.
  - Le parole chiave servono all'import e alla sincronizzazione: se la descrizione di un movimento ne contiene una, il movimento finisce in questa categoria.
  - La priorità decide chi vince quando più categorie corrispondono: vince il numero più basso.
- **Sincronizzare le categorie** su un periodo scelto (dal/al): l'app riapplica le parole chiave ai movimenti già presenti in quelle date e al termine indica quanti ne ha riclassificati. Utile dopo aver aggiunto o modificato parole chiave.
  - Se un movimento contiene parole chiave di più categorie, vince quella con priorità più alta (numero più basso).
  - I trasferimenti tra conti non vengono mai modificati, e i movimenti senza parole chiave riconosciute mantengono la categoria attuale.
- Eliminare una categoria (non è possibile eliminare quella predefinita; i movimenti di una categoria eliminata passano alla categoria predefinita).
