namespace RDS.ExpenseTracker.Domain.Dtos.Requests;

// Operazione su più movimenti dell'utente, identificati per id.
public class TransactionBulkRequest
{
    public List<int> Ids { get; set; } = [];
}

// Assegna la stessa categoria (0 = nessuna) a tutti i movimenti indicati.
public class TransactionBulkCategoryRequest : TransactionBulkRequest
{
    public int CategoryId { get; set; }
}
