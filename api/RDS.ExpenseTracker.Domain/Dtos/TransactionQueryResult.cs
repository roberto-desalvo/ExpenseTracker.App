using RDS.ExpenseTracker.Domain.Dtos;

namespace RDS.ExpenseTracker.Api.Dtos;

public class TransactionQueryResult : PagedResult<TransactionDto>
{
    public decimal TotalIncomes { get; set; }
    public decimal TotalOutcomes { get; set; }
    public decimal TotalNet { get; set; }
}