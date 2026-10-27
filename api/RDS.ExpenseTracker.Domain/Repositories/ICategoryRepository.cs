using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Domain.Repositories;

// Le categorie sono per utente: tutte le letture sono filtrate per userId.
public interface ICategoryRepository : IRepositoryBase
{
    Task<Category?> GetCategory(int id, int userId);
    Task<Category?> GetDefaultCategory(int userId);
    Task<int?> GetCategoryIdByKind(int userId, CategoryEnum kind);
    Task<IEnumerable<Category>> GetCategories(int userId, string? name = null);
    Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedCategories(CategoryQueryRequest request, int userId);
    Task AddCategories(IEnumerable<Category> categories);
    Task UpdateCategory(Category category);
    Task RemoveCategory(int id);
    Task RemoveCategory(Category category);

    // Elimina tutte le categorie dell'utente (reset demo/test); i movimenti collegati restano senza categoria.
    Task DeleteCategories(int userId);
    Task ReassignTransactionsToCategory(int sourceCategoryId, int targetCategoryId);
}
