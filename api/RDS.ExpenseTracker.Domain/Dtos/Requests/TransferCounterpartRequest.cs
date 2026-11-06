namespace RDS.ExpenseTracker.Domain.Dtos.Requests;

// Movimento da collegare come ramo mancante del trasferimento di un altro movimento.
public class TransferCounterpartRequest
{
    public int CounterpartId { get; set; }
}
