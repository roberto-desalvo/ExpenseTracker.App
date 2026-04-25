namespace RDS.ExpenseTracker.Api.Dtos;

public class TransactionQueryRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}