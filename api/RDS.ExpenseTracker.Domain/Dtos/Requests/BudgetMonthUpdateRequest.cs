namespace RDS.ExpenseTracker.Domain.Dtos.Requests;

// Previsioni complete di un mese: le categorie assenti o con importo nullo/zero restano senza previsione.
public class BudgetMonthUpdateRequest
{
    public DateTime Month { get; set; }
    public List<BudgetAmountRequest> Lines { get; set; } = [];
}

public class BudgetAmountRequest
{
    public int CategoryId { get; set; }
    public decimal? Amount { get; set; }
}
