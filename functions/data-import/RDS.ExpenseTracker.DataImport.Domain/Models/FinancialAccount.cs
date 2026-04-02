
namespace RDS.ExpenseTracker.Domain.Models
{
    public class FinancialAccount
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Availability { get; set; }
        public string? Description { get; set; }
    }
}
