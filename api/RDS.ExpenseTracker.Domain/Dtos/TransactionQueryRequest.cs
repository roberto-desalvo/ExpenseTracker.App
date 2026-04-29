namespace RDS.ExpenseTracker.Api.Dtos;

public class TransactionQueryRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int[]? IdAccounts { get; set; }
    public int[]? IdCategories { get; set; }
    public bool IncludeMoneyTransfers { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}