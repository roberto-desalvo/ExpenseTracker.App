using RDS.ExpenseTracker.Application.Extensions;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Application.Utilities;

/// <summary>
/// Assegna una categoria a partire dalla descrizione di un movimento, cercando i tag delle categorie
/// (match case-insensitive, per sottostringa). Se più categorie corrispondono vince quella con
/// priorità maggiore, cioè il valore di <see cref="Category.Priority"/> più basso; le categorie
/// senza priorità vengono per ultime. Condiviso da import e sincronizzazione categorie.
/// </summary>
public sealed class CategoryTagMatcher
{
    private static readonly char[] TagSeparators = [',', ';'];

    private readonly IReadOnlyList<(int CategoryId, string[] Tags)> _rules;

    public CategoryTagMatcher(IEnumerable<Category> categories)
    {
        _rules = categories
            .OrderBy(c => c.Priority ?? int.MaxValue)
            .ThenBy(c => c.Id)
            .Select(c => (c.Id, SplitTags(c.Tags).ToArray()))
            .Where(rule => rule.Item2.Length > 0)
            .ToList();
    }

    /// <summary>Id della categoria corrispondente, oppure <c>null</c> se nessun tag compare nella descrizione.</summary>
    public int? Match(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        foreach (var (categoryId, tags) in _rules)
        {
            if (description.ContainsOne(ignoreCase: true, tags))
                return categoryId;
        }

        return null;
    }

    public int MatchOrDefault(string? description, int defaultCategoryId)
        => Match(description) ?? defaultCategoryId;

    /// <summary>I tag sono salvati come stringa unica separata da virgola o punto e virgola.</summary>
    public static IEnumerable<string> SplitTags(string? tags)
        => string.IsNullOrWhiteSpace(tags)
            ? []
            : tags.Split(TagSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
