namespace RDS.ExpenseTracker.Domain.Dtos;

public class CategoryQueryRequest : PagedQueryRequest
{
    public string? Name { get; set; }
}
