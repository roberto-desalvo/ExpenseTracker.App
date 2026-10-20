namespace RDS.ExpenseTracker.Domain.Dtos;

public class TransferReconciliationResultDto
{
    public bool DryRun { get; set; }
    public int UsersProcessed { get; set; }
    public int ExaminedTransactions { get; set; }
    public int LinkedTransfers { get; set; }
    public int MatchedByRule { get; set; }
    public int MatchedByIban { get; set; }
    public int MatchedByCategory { get; set; }

    // Movimenti "Money transfers" con più controparti equivalenti: lasciati scollegati per non indovinare.
    public int AmbiguousSkipped { get; set; }

    public IEnumerable<TransferReconciliationPairDto> Pairs { get; set; } = [];
}

public class TransferReconciliationPairDto
{
    public int UserId { get; set; }
    public int OutboundTransactionId { get; set; }
    public int InboundTransactionId { get; set; }
    public string FromAccount { get; set; } = string.Empty;
    public string ToAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? OutboundDate { get; set; }
    public DateTime? InboundDate { get; set; }

    /// <summary>Criterio che ha prodotto il match: <c>Rule</c>, <c>Iban</c> o <c>Category</c>.</summary>
    public string Method { get; set; } = string.Empty;
}
