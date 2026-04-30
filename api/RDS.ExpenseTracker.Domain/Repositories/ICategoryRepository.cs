using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.Domain.Repositories;

public interface ICategoryRepository : IRepositoryBase
{
    Task<Category?> GetCategory(int id);
    Task<Category?> GetDefaultCategory();
    Task<IEnumerable<Category>> GetCategories(string? name = null);
    Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedCategories(CategoryQueryRequest request);
    Task AddCategories(IEnumerable<Category> categories);
    Task UpdateCategory(Category category);
    Task RemoveCategory(int id);
    Task RemoveCategory(Category category);
    Task ReassignTransactionsToCategory(int sourceCategoryId, int targetCategoryId);
}
