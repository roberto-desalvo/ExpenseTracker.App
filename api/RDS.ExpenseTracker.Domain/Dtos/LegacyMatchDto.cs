namespace RDS.ExpenseTracker.Domain.Dtos;

// Abbinamento dello storico Excel (Spese.xlsx): un foglio = un mese.

public class LegacySheetDto
{
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
}

public class LegacyExcelRowDto
{
    // Numero di riga nel foglio (1-based): identifica la riga nella pagina.
    public int RowNumber { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    // Firmato come Transaction.Amount: uscite negative, entrate positive.
    public decimal Amount { get; set; }
    public string? AccountName { get; set; }
    public int? AccountId { get; set; }
    public string? ExcelCategory { get; set; }
    public int? SuggestedCategoryId { get; set; }
    // Riga di un conto senza import automatico (Contanti, Hype): va creata come nuovo movimento, non abbinata.
    public bool ToCreate { get; set; }
}

public class LegacyDbTransactionDto
{
    public int Id { get; set; }
    public DateTime? Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int AccountId { get; set; }
    public string Account { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? Category { get; set; }
    public bool IsTransfer { get; set; }
    public DateTime? LegacyMatchedAt { get; set; }
}

public class LegacyMatchSuggestionDto
{
    public int RowNumber { get; set; }
    public int TransactionId { get; set; }
}

public class LegacyMonthDto
{
    public LegacySheetDto Sheet { get; set; } = new();
    public List<LegacyExcelRowDto> ExcelRows { get; set; } = [];
    public List<LegacyDbTransactionDto> DbTransactions { get; set; } = [];
    public List<LegacyMatchSuggestionDto> Suggestions { get; set; } = [];
}

// Esito del ripristino di un foglio: movimenti riportati allo stato di import e movimenti creati eliminati.
public class LegacyResetResultDto
{
    public int Restored { get; set; }
    public int Deleted { get; set; }
}
