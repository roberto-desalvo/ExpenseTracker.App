namespace RDS.ExpenseTracker.Domain.Entities;

public class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = [];
}
