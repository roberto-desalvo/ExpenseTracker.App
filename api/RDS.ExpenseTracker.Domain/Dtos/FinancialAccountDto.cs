namespace RDS.ExpenseTracker.Api.Dtos
{
    public class FinancialAccountDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal? Availability { get; set; }
        public string? Description { get; set; }
    }
}
