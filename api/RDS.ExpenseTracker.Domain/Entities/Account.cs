namespace RDS.ExpenseTracker.Domain.Entities;

public class Account(int id, string name, int userId)
{
    public int Id { get; set; } = id;
    public string Name { get; set; } = name ?? throw new ArgumentNullException(nameof(name));
    public int UserId { get; set; } = userId;

    // IBAN del conto (normalizzato: maiuscolo, senza spazi). Usato dagli import per riconoscere i trasferimenti.
    public string? Iban { get; set; }

    public User? UserNavigation { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = [];
}
