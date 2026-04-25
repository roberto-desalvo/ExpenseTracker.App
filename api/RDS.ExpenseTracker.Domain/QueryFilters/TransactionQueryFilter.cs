namespace RDS.ExpenseTracker.Domain.QueryFilters;

public class TransactionQueryFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
