using RDS.ExpenseTracker.Application.Builders;
using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public int? Priority { get; set; }
    public bool? IsDefault { get; set; }
    public string? Tags { get; set; } = string.Empty;

    public ICollection<Transaction> Transactions { get; set; } = [];

    public static IEnumerable<Category> CreateSeedCategories()
        => Enum.GetValues<CategoryEnum>()
            .Select(CategorySeedBuilder.Build)
            .ToArray();
}
