namespace RDS.ExpenseTracker.Domain.Dtos;

public class TransactionQueryRequest : PagedQueryRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int[]? IdAccounts { get; set; }
    public int[]? IdCategories { get; set; }
    public bool? IsIncome { get; set; }
}