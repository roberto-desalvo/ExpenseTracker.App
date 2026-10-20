namespace RDS.ExpenseTracker.Domain.Dtos.Requests;

public class TransactionQueryRequest : PagedQueryRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int[]? IdAccounts { get; set; }
    public int[]? IdCategories { get; set; }
    public bool? IsIncome { get; set; }

    /// <summary>Campo di ordinamento: date (default), description, category, account, amount.</summary>
    public string? SortBy { get; set; }

    /// <summary>Direzione di ordinamento: asc o desc (default).</summary>
    public string? SortDirection { get; set; }
}
