namespace RDS.ExpenseTracker.Domain.Dtos;

public class AccountDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Iban { get; set; }

    // Tipo di conto (banca); determina quale import scrive su questo conto. Al massimo un conto per tipo.
    public int? AccountTypeId { get; set; }

    // Il client non può impostarlo: viene sempre forzato server-side.
    public int UserId { get; set; }
}
