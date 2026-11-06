namespace RDS.ExpenseTracker.Domain.Dtos.Requests;

public class LegacyMatchApplyRequest
{
    public List<LegacyMatchApplyItem> Items { get; set; } = [];
}

public class LegacyMatchApplyItem
{
    public int TransactionId { get; set; }
    public string Description { get; set; } = string.Empty;
    // null = nessuna categoria.
    public int? CategoryId { get; set; }
}
