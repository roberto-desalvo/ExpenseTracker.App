namespace RDS.ExpenseTracker.Domain.Dtos.Requests;

public class CategoryQueryRequest : PagedQueryRequest
{
    public string? Name { get; set; }
}
