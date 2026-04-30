namespace RDS.ExpenseTracker.Domain.Dtos;

public class AccountQueryRequest : PagedQueryRequest
{
    public string? Name { get; set; }
}
