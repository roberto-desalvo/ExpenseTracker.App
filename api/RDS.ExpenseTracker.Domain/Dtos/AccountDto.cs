namespace RDS.ExpenseTracker.Domain.Dtos;

public class AccountDto
{
    public int Id { get; set; }
    public string? Name { get; set; }

    // Il client non può impostarlo: viene sempre forzato server-side.
    public int UserId { get; set; }
}
