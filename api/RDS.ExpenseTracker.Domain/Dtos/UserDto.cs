namespace RDS.ExpenseTracker.Domain.Dtos;

public class UserDto
{
    public int Id { get; set; }
    public string? AzureOid { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsDemo { get; set; }
}
