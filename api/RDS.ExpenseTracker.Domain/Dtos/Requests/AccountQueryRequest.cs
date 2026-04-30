namespace RDS.ExpenseTracker.Domain.Dtos.Requests;

public class AccountQueryRequest : PagedQueryRequest
{
    public string? Name { get; set; }
}
