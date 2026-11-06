namespace RDS.ExpenseTracker.Domain.Dtos.Requests;

// Ramo mancante del trasferimento di un altro movimento: un movimento esistente (CounterpartId)
// oppure uno nuovo da creare (NewCounterpart).
public class TransferCounterpartRequest
{
    public int CounterpartId { get; set; }
    public NewTransferCounterpart? NewCounterpart { get; set; }
}

// Movimento creato come ramo mancante: importo opposto e data del movimento di partenza, categoria Money transfers.
public class NewTransferCounterpart
{
    public int AccountId { get; set; }

    /// <summary>Vuota = descrizione del movimento di partenza.</summary>
    public string? Description { get; set; }
}
