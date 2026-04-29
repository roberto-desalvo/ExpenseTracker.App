namespace RDS.ExpenseTracker.Domain.Entities;

public class Account(int id, string name)
{
    public int Id { get; set; } = id;
    public string Name { get; set; } = name ?? throw new ArgumentNullException(nameof(name));

    public ICollection<Transaction> Transactions { get; set; } = [];
}
