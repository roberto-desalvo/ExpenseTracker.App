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

    // Le categorie appartengono a un utente: ognuno ha il proprio set, creato dalle categorie di default.
    public int UserId { get; set; }

    // Categoria di sistema da cui deriva (es. MoneyTransfers), usata dal codice per trovare l'id della categoria
    // dell'utente; null per le categorie create dall'utente.
    public CategoryEnum? Kind { get; set; }

    public User? UserNavigation { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = [];

    public static IEnumerable<Category> CreateDefaultCategories(int userId)
        => Enum.GetValues<CategoryEnum>()
            .Select(kind => CategorySeedBuilder.Build(kind, userId))
            .ToArray();
}
