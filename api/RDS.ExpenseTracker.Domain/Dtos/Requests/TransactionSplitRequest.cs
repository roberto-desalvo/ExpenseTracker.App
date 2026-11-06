namespace RDS.ExpenseTracker.Domain.Dtos.Requests;

// Scomposizione di un movimento: importi delle voci risultanti (il primo resta sul movimento originale).
// La somma deve essere pari all'importo originale; ogni importo ha lo stesso segno.
public class TransactionSplitRequest
{
    public List<decimal> Amounts { get; set; } = [];
}
