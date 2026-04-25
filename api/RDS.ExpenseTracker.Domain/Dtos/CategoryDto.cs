namespace RDS.ExpenseTracker.Api.Dtos
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public int Priority { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public IEnumerable<string> Tags { get; set; } = Enumerable.Empty<string>();
    }
}
