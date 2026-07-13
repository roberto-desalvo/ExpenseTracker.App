namespace RDS.ExpenseTracker.Domain.Entities;

public class User(int id, string email, bool isDemo = false)
{
    public int Id { get; set; } = id;
    public string? AzureOid { get; set; }
    public string Email { get; set; } = email ?? throw new ArgumentNullException(nameof(email));
    public bool IsDemo { get; set; } = isDemo;

    public ICollection<Account> Accounts { get; set; } = [];
}
