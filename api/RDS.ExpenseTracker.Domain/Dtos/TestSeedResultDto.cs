namespace RDS.ExpenseTracker.Domain.Dtos;

public class TestSeedResultDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool UserCreated { get; set; }
    public int AccountCount { get; set; }
    public int TransactionCount { get; set; }
    public int TransferCount { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}
