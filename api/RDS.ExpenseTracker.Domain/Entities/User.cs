namespace RDS.ExpenseTracker.Domain.Entities;

public class User(int id, string email, bool isDemo = false)
{
    public int Id { get; set; } = id;
    public string? AzureOid { get; set; }
    public string? AppOid { get; set; }
    public string Email { get; set; } = email ?? throw new ArgumentNullException(nameof(email));
    public bool IsDemo { get; set; } = isDemo;

    // Abilita le funzioni di amministrazione (es. abbinamento dello storico Excel).
    public bool IsAdmin { get; set; }

    // Layout della pagina Analisi in JSON (AnalyticsLayoutDto); null = layout predefinito.
    public string? AnalyticsLayout { get; set; }

    public ICollection<Account> Accounts { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
}
