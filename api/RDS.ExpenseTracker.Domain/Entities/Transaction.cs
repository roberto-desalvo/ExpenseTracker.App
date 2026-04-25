namespace RDS.ExpenseTracker.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? Date {  get; set; }
    public int AccountId { get; set; }
    public int? CategoryId { get; set; }
    public int? TransferId { get; set; }

    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }

    public Account AccountNavigation { get; set; } = default!;
    public Transfer? TransferNavigation { get; set; }
    public Category? CategoryNavigation { get; set; }
}
