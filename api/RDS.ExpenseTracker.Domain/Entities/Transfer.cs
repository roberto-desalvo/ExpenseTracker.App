using System.ComponentModel.DataAnnotations.Schema;

namespace RDS.ExpenseTracker.Domain.Entities;

public class Transfer
{
    public int Id { get; set; }
    public DateTime CreatedOn { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = [];

    [NotMapped]
    public IEnumerable<Transaction> TransactionFroms => Transactions.Where(t => t.Amount < 0m);

    [NotMapped]
    public IEnumerable<Transaction> TransactionTos => Transactions.Where(t => t.Amount > 0m);
}
