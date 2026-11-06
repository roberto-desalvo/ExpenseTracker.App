namespace RDS.ExpenseTracker.Domain.Dtos.Requests;

public class TransactionQueryRequest : PagedQueryRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int[]? IdAccounts { get; set; }
    public int[]? IdCategories { get; set; }
    public bool? IsIncome { get; set; }

    /// <summary>Solo i trasferimenti: movimenti collegati a un Transfer o nella categoria "Money transfers".</summary>
    public bool? OnlyTransfers { get; set; }

    /// <summary>Solo i trasferimenti incompleti: senza il ramo di segno opposto, o in "Money transfers" senza trasferimento.</summary>
    public bool? OnlyIncompleteTransfers { get; set; }

    /// <summary>Testo cercato nella descrizione (corrispondenza parziale, case insensitive).</summary>
    public string? Search { get; set; }

    /// <summary>Campo di ordinamento: date (default), description, category, account, amount.</summary>
    public string? SortBy { get; set; }

    /// <summary>Direzione di ordinamento: asc o desc (default).</summary>
    public string? SortDirection { get; set; }
}
