/*
  Riconcilia i duplicati creati dagli import per i movimenti modificati a mano prima della correzione
  di TransactionService.UpdateTransaction (la modifica azzerava ExternalId, TransferId e CreatedOn).

  Movimento "danneggiato": ExternalId NULL e CreatedOn < 1900 (impronta del bug).
  Duplicato: movimento con ExternalId sullo stesso conto, stessa data (giorno) e stesso importo.
  A parità di chiave i danneggiati sono abbinati ai candidati più recenti (CreatedOn decrescente), 1:1.

  Per ogni coppia si tiene il movimento danneggiato (descrizione e categoria modificate a mano) e:
    - riceve l'ExternalId del duplicato (così il prossimo import lo riconosce);
    - riceve il TransferId del duplicato, se lui non ne ha uno;
    - CreatedOn = CreatedOn del duplicato, UpdatedOn = adesso;
  poi il duplicato viene eliminato.
  Coppie in cui entrambi sono collegati a trasferimenti diversi vengono solo segnalate, non toccate.

  Uso: eseguire dopo l'import. Con @Apply = 0 (default) mostra solo l'anteprima;
  con @Apply = 1 applica le modifiche in un'unica transazione.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @UserId int = NULL;          -- NULL = tutti gli utenti
DECLARE @ImportedFrom datetime2 = NULL; -- opzionale: considera duplicati solo i movimenti creati da questa data (UTC)
DECLARE @Apply bit = 0;              -- 0 = anteprima, 1 = applica

DROP TABLE IF EXISTS #Damaged;
DROP TABLE IF EXISTS #Candidates;
DROP TABLE IF EXISTS #Pairs;

SELECT t.Id, t.AccountId, CAST(t.Date AS date) AS [Day], t.Amount, t.TransferId,
       ROW_NUMBER() OVER (PARTITION BY t.AccountId, CAST(t.Date AS date), t.Amount ORDER BY t.Id) AS Rn
INTO #Damaged
FROM dbo.Transactions t
JOIN dbo.Accounts a ON a.Id = t.AccountId
WHERE t.ExternalId IS NULL
  AND t.CreatedOn < '1900-01-01'
  AND (@UserId IS NULL OR a.UserId = @UserId);

SELECT t.Id, t.AccountId, CAST(t.Date AS date) AS [Day], t.Amount, t.ExternalId, t.TransferId, t.CreatedOn,
       ROW_NUMBER() OVER (PARTITION BY t.AccountId, CAST(t.Date AS date), t.Amount ORDER BY t.CreatedOn DESC, t.Id DESC) AS Rn
INTO #Candidates
FROM dbo.Transactions t
WHERE t.ExternalId IS NOT NULL
  AND (@ImportedFrom IS NULL OR t.CreatedOn >= @ImportedFrom)
  AND EXISTS (SELECT 1 FROM #Damaged d
              WHERE d.AccountId = t.AccountId AND d.[Day] = CAST(t.Date AS date) AND d.Amount = t.Amount);

SELECT d.Id AS KeepId, c.Id AS DuplicateId, d.AccountId, d.[Day], d.Amount,
       c.ExternalId, d.TransferId AS KeepTransferId, c.TransferId AS DuplicateTransferId, c.CreatedOn AS DuplicateCreatedOn,
       CASE WHEN d.TransferId IS NOT NULL AND c.TransferId IS NOT NULL AND d.TransferId <> c.TransferId
            THEN 1 ELSE 0 END AS TransferConflict
INTO #Pairs
FROM #Damaged d
JOIN #Candidates c ON c.AccountId = d.AccountId AND c.[Day] = d.[Day] AND c.Amount = d.Amount AND c.Rn = d.Rn;

-- Anteprima ----------------------------------------------------------------
SELECT p.KeepId, p.DuplicateId, acc.Name AS Account, p.[Day], p.Amount,
       k.Description AS KeepDescription, dup.Description AS DuplicateDescription,
       kc.Name AS KeepCategory, dc.Name AS DuplicateCategory,
       p.ExternalId, p.KeepTransferId, p.DuplicateTransferId,
       CASE WHEN p.TransferConflict = 1 THEN 'SALTATA: trasferimenti diversi' ELSE 'unisci' END AS Action
FROM #Pairs p
JOIN dbo.Transactions k ON k.Id = p.KeepId
JOIN dbo.Transactions dup ON dup.Id = p.DuplicateId
JOIN dbo.Accounts acc ON acc.Id = p.AccountId
LEFT JOIN dbo.Categories kc ON kc.Id = k.CategoryId
LEFT JOIN dbo.Categories dc ON dc.Id = dup.CategoryId
ORDER BY acc.Name, p.[Day], p.KeepId;

-- Movimenti danneggiati senza duplicato: verranno reimportati al prossimo import (rieseguire allora lo script).
SELECT d.Id, acc.Name AS Account, d.[Day], d.Amount, t.Description
FROM #Damaged d
JOIN dbo.Transactions t ON t.Id = d.Id
JOIN dbo.Accounts acc ON acc.Id = d.AccountId
WHERE NOT EXISTS (SELECT 1 FROM #Pairs p WHERE p.KeepId = d.Id)
ORDER BY acc.Name, d.[Day];

SELECT COUNT(*) AS Pairs,
       SUM(CASE WHEN TransferConflict = 0 THEN 1 ELSE 0 END) AS ToMerge,
       SUM(TransferConflict) AS Skipped
FROM #Pairs;

IF @Apply = 0
BEGIN
    PRINT 'Anteprima: nessuna modifica. Impostare @Apply = 1 per applicare.';
    RETURN;
END

-- Applicazione -------------------------------------------------------------
BEGIN TRANSACTION;

-- prima si elimina il duplicato: l'indice unico (AccountId, ExternalId) impedisce di avere l'ID su entrambi
DELETE t
FROM dbo.Transactions t
JOIN #Pairs p ON p.DuplicateId = t.Id
WHERE p.TransferConflict = 0;

UPDATE t
SET t.ExternalId = p.ExternalId,
    t.TransferId = COALESCE(t.TransferId, p.DuplicateTransferId),
    t.CreatedOn  = p.DuplicateCreatedOn,
    t.UpdatedOn  = SYSUTCDATETIME()
FROM dbo.Transactions t
JOIN #Pairs p ON p.KeepId = t.Id
WHERE p.TransferConflict = 0;

COMMIT TRANSACTION;

-- Trasferimenti rimasti con una sola gamba (es. quello a cui era collegato il movimento prima del bug):
-- da ricollegare o eliminare a mano.
SELECT tr.Id AS TransferId, tr.CreatedOn, MIN(t.Id) AS TransactionId, MIN(acc.Name) AS Account, MIN(t.Amount) AS Amount
FROM dbo.Transfers tr
JOIN dbo.Transactions t ON t.TransferId = tr.Id
JOIN dbo.Accounts acc ON acc.Id = t.AccountId
WHERE (@UserId IS NULL OR acc.UserId = @UserId)
GROUP BY tr.Id, tr.CreatedOn
HAVING COUNT(*) = 1;
