using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Application.Utilities;

/// <summary>
/// Categorie di un utente viste dal codice: risolve l'id della categoria di sistema per
/// <see cref="CategoryEnum"/> (tramite <see cref="Category.Kind"/>), la categoria predefinita
/// e il matcher dei tag. Gli id sono <c>null</c> se l'utente non ha (più) quella categoria.
/// </summary>
public sealed class UserCategories
{
    private readonly Dictionary<CategoryEnum, int> _idsByKind;

    public UserCategories(IEnumerable<Category> categories)
    {
        var list = categories.ToList();

        _idsByKind = list
            .Where(c => c.Kind.HasValue)
            .GroupBy(c => c.Kind!.Value)
            .ToDictionary(g => g.Key, g => g.Min(c => c.Id));

        DefaultId = list.Where(c => c.IsDefault == true).OrderBy(c => c.Id).Select(c => (int?)c.Id).FirstOrDefault()
            ?? IdOf(CategoryEnum.Default);

        Matcher = new CategoryTagMatcher(list);
    }

    public int? DefaultId { get; }

    public int? MoneyTransfersId => IdOf(CategoryEnum.MoneyTransfers);

    public CategoryTagMatcher Matcher { get; }

    public int? IdOf(CategoryEnum kind) => _idsByKind.TryGetValue(kind, out var id) ? id : null;

    /// <summary>Categoria per tag della descrizione, altrimenti la predefinita.</summary>
    public int? MatchOrDefault(string? description) => Matcher.Match(description) ?? DefaultId;
}
