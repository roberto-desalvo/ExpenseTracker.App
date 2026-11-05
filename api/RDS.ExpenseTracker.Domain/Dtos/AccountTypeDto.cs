namespace RDS.ExpenseTracker.Domain.Dtos;

public class AccountTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // True se gli estratti conto di questo tipo si possono importare da file.
    public bool SupportsImport { get; set; }
}
