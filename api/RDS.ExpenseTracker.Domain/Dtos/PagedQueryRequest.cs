namespace RDS.ExpenseTracker.Domain.Dtos;

public abstract class PagedQueryRequest
{
    public int Page { get; set; } = 0;
    public int PageSize { get; set; } = 25;
}
