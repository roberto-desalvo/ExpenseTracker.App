using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Tests;

/// <summary>
/// Repository categorie in memoria condiviso dai test. <see cref="WithDefaults"/> crea le categorie di default
/// di un utente con Id = valore di <see cref="CategoryEnum"/>, così i test possono confrontare gli id con l'enum.
/// </summary>
internal sealed class FakeCategoryRepository(IEnumerable<Category>? categories = null) : ICategoryRepository
{
    public List<Category> Categories { get; } = [.. categories ?? []];
    public int SaveCount { get; private set; }

    public static FakeCategoryRepository WithDefaults(int userId)
        => new(Category.CreateDefaultCategories(userId).Select(c => { c.Id = (int)c.Kind!.Value; return c; }));

    private IEnumerable<Category> ForUser(int userId) => Categories.Where(c => c.UserId == userId);

    public Task<IEnumerable<Category>> GetCategories(int userId, string? name = null)
        => Task.FromResult<IEnumerable<Category>>(ForUser(userId).ToList());

    public Task<Category?> GetCategory(int id, int userId)
        => Task.FromResult(ForUser(userId).FirstOrDefault(c => c.Id == id));

    public Task<Category?> GetDefaultCategory(int userId)
        => Task.FromResult(ForUser(userId).FirstOrDefault(c => c.IsDefault == true));

    public Task<int?> GetCategoryIdByKind(int userId, CategoryEnum kind)
        => Task.FromResult(ForUser(userId).Where(c => c.Kind == kind).Select(c => (int?)c.Id).FirstOrDefault());

    // Simula l'identity: gli id nuovi partono dopo il massimo esistente.
    public Task AddCategories(IEnumerable<Category> categories)
    {
        foreach (var category in categories)
        {
            category.Id = Categories.Count == 0 ? 100 : Categories.Max(c => c.Id) + 1;
            Categories.Add(category);
        }
        return Task.CompletedTask;
    }

    public Task DeleteCategories(int userId)
    {
        Categories.RemoveAll(c => c.UserId == userId);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }

    public Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedCategories(CategoryQueryRequest request, int userId) => throw new NotImplementedException();
    public Task UpdateCategory(Category category) => throw new NotImplementedException();
    public Task RemoveCategory(int id) => throw new NotImplementedException();
    public Task RemoveCategory(Category category) => throw new NotImplementedException();
    public Task ReassignTransactionsToCategory(int sourceCategoryId, int targetCategoryId) => throw new NotImplementedException();
}
