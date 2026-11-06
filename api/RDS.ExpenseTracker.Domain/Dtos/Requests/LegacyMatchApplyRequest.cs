namespace RDS.ExpenseTracker.Domain.Dtos.Requests;

public class LegacyMatchApplyRequest
{
    public List<LegacyMatchApplyItem> Items { get; set; } = [];
}

// Foglio (mese) di cui annullare l'abbinamento allo storico Excel.
public class LegacyResetRequest
{
    public int Year { get; set; }
    public int Month { get; set; }
}

public class LegacyCreateRequest
{
    public List<LegacyCreateItem> Items { get; set; } = [];
}

public class LegacyCreateItem
{
    public int AccountId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    // null = nessuna categoria.
    public int? CategoryId { get; set; }
}

public class LegacyMatchApplyItem
{
    public int TransactionId { get; set; }
    public string Description { get; set; } = string.Empty;
    // null = nessuna categoria.
    public int? CategoryId { get; set; }
}
