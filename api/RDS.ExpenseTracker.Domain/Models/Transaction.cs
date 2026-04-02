namespace RDS.ExpenseTracker.Domain.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public int CategoryId { get; set; }
        public string CategoryDescription { get; set; } = string.Empty;
        public int FinancialAccountId { get; set; }
        public string FinancialAccountName { get; set; } = string.Empty;
        public bool IsTransfer { get; set; }
    }
}
